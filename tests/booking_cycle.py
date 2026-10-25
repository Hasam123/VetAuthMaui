"""Integration checks against a disposable copy of the local MySQL schema.

Run with Python 3. No third-party Python modules required. Does not write to
vet_clinic: only its schema, doctors and services are read.
"""
import json
import shutil
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MYSQL = 'C:/xampp/mysql/bin/mysql.exe'
DUMP = 'C:/xampp/mysql/bin/mysqldump.exe'
PHP = 'C:/xampp/php/php.exe'
DB = 'vet_clinic_qa_' + str(time.time_ns())
checks = []


def sql(query):
    return subprocess.check_output([MYSQL, '-u', 'root', '--default-character-set=utf8mb4', '-N', '-B', '-e', query], text=True, encoding='utf-8').strip()


def check(name, condition):
    checks.append((name, bool(condition)))
    print(('PASS ' if condition else 'FAIL ') + name, flush=True)


def main():
    sql(f'CREATE DATABASE `{DB}` CHARACTER SET utf8mb4')
    try:
        schema = subprocess.check_output([DUMP, '-u', 'root', '--no-data', 'vet_clinic'])
        subprocess.run([MYSQL, '-u', 'root', DB], input=schema, check=True)
        sql(f'INSERT INTO `{DB}`.admins SELECT * FROM vet_clinic.admins; INSERT INTO `{DB}`.services SELECT * FROM vet_clinic.services;')
        with tempfile.TemporaryDirectory(prefix='vet_booking_qa_') as directory:
            app = Path(directory)
            shutil.copytree(ROOT / 'api', app / 'api')
            connection = f'<?php mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT); $conn = new mysqli("localhost", "root", "", "{DB}"); $conn->set_charset("utf8mb4");'
            (app / 'db.php').write_text(connection, encoding='utf-8')
            (app / 'api/db.php').write_text(connection, encoding='utf-8')
            with socket.socket() as sock:
                sock.bind(('127.0.0.1', 0))
                port = sock.getsockname()[1]
            with (app / 'server.log').open('w') as log:
                server = subprocess.Popen([PHP, '-S', f'127.0.0.1:{port}', '-t', str(app)], stdout=log, stderr=log)
                try:
                    base = f'http://127.0.0.1:{port}/api/'

                    def api(path, data=None):
                        payload = None if data is None else json.dumps(data).encode()
                        req = urllib.request.Request(base + path, data=payload, headers={'Content-Type': 'application/json'})
                        try:
                            response = urllib.request.urlopen(req, timeout=10)
                        except urllib.error.HTTPError as error:
                            response = error
                        raw = response.read().decode('utf-8-sig')
                        try:
                            return json.loads(raw)
                        except ValueError:
                            return {'success': False, 'invalid_json': True, 'raw': raw[:250]}

                    for attempt in range(40):
                        try:
                            doctors = api('doctors/list.php')['doctors']
                            break
                        except (OSError, KeyError):
                            time.sleep(0.1)
                    else:
                        raise RuntimeError('Test API did not start')
                    ids = {row.split('\t')[0]: int(row.split('\t')[1]) for row in sql(f"SELECT login,id FROM `{DB}`.admins WHERE login IN ('admin1','admin2')").splitlines()}
                    d1, d2 = ids['admin1'], ids['admin2']
                    service = api('services/list.php')['services'][0]['id']
                    phone = '000000000001'
                    client = {'name': 'QA client', 'phone': phone, 'password': 'qa_password'}
                    check('register client', api('clients/register.php', client).get('success'))
                    check('reject duplicate registration', not api('clients/register.php', client).get('success'))
                    check('client login', api('clients/login.php', client).get('success'))
                    check('reject wrong password', not api('clients/login.php', dict(client, password='wrong')).get('success'))
                    check('create pet', api('pets/create.php', {'phone': phone, 'name': 'QA pet', 'type': 'Кошка', 'age': '2'}).get('success'))
                    pet = api('pets/list.php?phone=' + phone)['pets'][0]['id']
                    slots = api(f'schedule/free_slots.php?doctor_id={d1}')['days']
                    times = [s['time'] for day in slots for s in day['slots'] if s['is_available']]
                    booking = dict(client, pet_name='QA pet', pet_type='Кошка', pet_age='2', pet_id=pet, service_id=service, doctor_id=d1, appointment_at=times[0], comment='QA booking')

                    def records():
                        return api('clients/profile.php?phone=' + phone)['requests']

                    def free(doctor, value):
                        return next(s['is_available'] for day in api(f'schedule/free_slots.php?doctor_id={doctor}')['days'] for s in day['slots'] if s['time'] == value)

                    check('book Ivanov', api('appointments/create.php', booking).get('success'))
                    appointment = records()[0]
                    aid = int(appointment['id'])
                    check('client sees selected doctor', int(appointment['doctor_id']) == d1 and bool(appointment['doctor_name']))
                    check('slot occupied for Ivanov', not free(d1, times[0]))
                    check('same slot free for Petrov', free(d2, times[0]))
                    check('reject duplicate booking', not api('appointments/create.php', booking).get('success'))
                    check('allow same time different doctor', api('appointments/create.php', dict(booking, doctor_id=d2)).get('success'))
                    for doctor in (d1, d2):
                        own = api(f'appointments/list.php?doctor_id={doctor}')['requests']
                        check(f'API list scoped to doctor {doctor}', len(own) == 1 and all(int(a['doctor_id']) == doctor for a in own))
                    check('accept booking', api('appointments/update_status.php', {'id': aid, 'status': 'accepted'}).get('success'))
                    check('save doctor comment', api('appointments/update_admin_comment.php', {'id': aid, 'admin_comment': 'QA comment'}).get('success'))
                    check('client sees comment', next(a for a in records() if int(a['id']) == aid)['admin_comment'] == 'QA comment')
                    check('reject invalid status', not api('appointments/update_status.php', {'id': aid, 'status': 'invalid'}).get('success'))
                    check('completion requires medical record', not api('appointments/update_status.php', {'id': aid, 'status': 'done'}).get('success'))
                    check('reject nonexistent status target', not api('appointments/update_status.php', {'id': 2147483647, 'status': 'accepted'}).get('success'))
                    check('reject nonexistent comment target', not api('appointments/update_admin_comment.php', {'id': 2147483647, 'admin_comment': 'test'}).get('success'))
                    medical = {'id': aid, 'jaloba': 'QA complaint', 'diagnoz': 'QA diagnosis', 'obsled_result': 'QA result', 'naz_lech': 'QA treatment', 'procedure_done': 'QA procedure', 'treatment_notes': 'QA notes'}
                    check('save medical record', api('appointments/update_medical_record.php', medical).get('success'))
                    done = next(a for a in records() if int(a['id']) == aid)
                    check('client sees completed medical record', done['status'] == 'done' and done['diagnoz'] == medical['diagnoz'] and done['naz_lech'] == medical['naz_lech'])
                    check('history snapshot created once', sql(f'SELECT COUNT(*) FROM `{DB}`.service_history WHERE appointment_id={aid}') == '1')
                    check('resave medical record', api('appointments/update_medical_record.php', medical).get('success'))
                    check('history not duplicated', sql(f'SELECT COUNT(*) FROM `{DB}`.service_history WHERE appointment_id={aid}') == '1')
                    check('completed booking cannot be reopened', not api('appointments/update_status.php', {'id': aid, 'status': 'new'}).get('success'))
                    check('reject cancel completed booking', not api('appointments/cancel.php', {'id': aid, 'phone': phone}).get('success'))
                    second = next(int(a['id']) for a in records() if int(a['doctor_id']) == d2)
                    check('reject cancellation by other client', not api('appointments/cancel.php', {'id': second, 'phone': '000000000002'}).get('success'))
                    check('cancel active booking', api('appointments/cancel.php', {'id': second, 'phone': phone}).get('success'))
                    check('cancelled slot free again', free(d2, times[0]))
                    check('rebook cancelled slot', api('appointments/create.php', dict(booking, doctor_id=d2)).get('success'))
                    before = sql(f'SELECT COUNT(*) FROM `{DB}`.pets')
                    bad = api('appointments/create.php', dict(booking, appointment_at=times[1], pet_id=0, pet_name='QA orphan', service_id=2147483647))
                    check('invalid service produces JSON failure', not bad.get('success') and not bad.get('invalid_json'))
                    check('failed booking creates no orphan pet', sql(f'SELECT COUNT(*) FROM `{DB}`.pets') == before)
                    check('reject past booking', not api('appointments/create.php', dict(booking, appointment_at='2000-01-01 08:00:00')).get('success'))
                    check('reject off-grid time', not api('appointments/create.php', dict(booking, appointment_at=times[1][:-5] + '15:00')).get('success'))
                    conflict = api('appointments/update_medical_record.php', dict(medical, id=second, diagnoz='Must not persist'))
                    check('cancelled booking cannot be completed', not conflict.get('success') and not conflict.get('invalid_json'))
                    check('failed medical save leaves no partial rows', sql(f'SELECT COUNT(*) FROM `{DB}`.result_obsled WHERE zapic_id={second}') == '0')
                    check('reject empty medical record', not api('appointments/update_medical_record.php', {'id': aid}).get('success'))
                    check('reject missing doctor', not api('appointments/create.php', dict(booking, doctor_id=0, appointment_at=times[2])).get('success'))
                    check('reject unknown doctor', not api('appointments/create.php', dict(booking, doctor_id=2147483647, appointment_at=times[2])).get('success'))
                    check('reject missing schedule doctor', not api('schedule/free_slots.php').get('success'))
                    check('reject nonexistent delete target', not api('appointments/delete.php', {'id': 2147483647}).get('success'))
                    # Deliberately fail after a pet has been inserted, to test real rollback.
                    sql(f"CREATE TRIGGER `{DB}`.qa_fail_booking BEFORE INSERT ON `{DB}`.zapic_na_priem FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='QA forced insert failure'")
                    before = sql(f'SELECT COUNT(*) FROM `{DB}`.pets')
                    failure = api('appointments/create.php', dict(booking, pet_id=0, pet_name='QA rollback', appointment_at=times[2]))
                    check('booking SQL failure returns JSON', not failure.get('success') and not failure.get('invalid_json'))
                    check('booking SQL failure rolls back new pet', sql(f'SELECT COUNT(*) FROM `{DB}`.pets') == before)
                    sql(f'DROP TRIGGER `{DB}`.qa_fail_booking')
                    check('create appointment for medical rollback', api('appointments/create.php', dict(booking, appointment_at=times[2])).get('success'))
                    rollback_id = next(int(a['id']) for a in records() if a['appointment_at'] == times[2])
                    sql(f"CREATE TRIGGER `{DB}`.qa_fail_treatment BEFORE INSERT ON `{DB}`.lechenie FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='QA forced treatment failure'")
                    failure = api('appointments/update_medical_record.php', dict(medical, id=rollback_id))
                    check('medical SQL failure returns JSON', not failure.get('success') and not failure.get('invalid_json'))
                    check('medical SQL failure rolls back examination', sql(f'SELECT COUNT(*) FROM `{DB}`.result_obsled WHERE zapic_id={rollback_id}') == '0')
                    check('medical SQL failure preserves status', sql(f'SELECT status FROM `{DB}`.zapic_na_priem WHERE id={rollback_id}') == 'new')
                    check('medical SQL failure creates no history', sql(f'SELECT COUNT(*) FROM `{DB}`.service_history WHERE appointment_id={rollback_id}') == '0')
                    sql(f'DROP TRIGGER `{DB}`.qa_fail_treatment')
                    check('delete test appointment', api('appointments/delete.php', {'id': rollback_id}).get('success'))
                    check('deleted appointment disappears from client view', all(int(a['id']) != rollback_id for a in records()))
                    check('deletion releases slot', free(d1, times[2]))
                    # Probe immutability only in the disposable QA database.
                    for operation in ['UPDATE service_history SET quantity=2', 'DELETE FROM service_history']:
                        result = subprocess.run([MYSQL, '-u', 'root', DB, '-e', operation + f' WHERE appointment_id={aid}'], capture_output=True)
                        check('history rejects ' + operation.split()[0], result.returncode != 0)
                finally:
                    server.terminate()
                    server.wait(timeout=10)
    finally:
        # Only the generated QA database may ever be removed.
        assert DB.startswith('vet_clinic_qa_') and DB[len('vet_clinic_qa_'):].isdigit()
        sql(f'DROP DATABASE `{DB}`')
    failed = sum(not ok for _, ok in checks)
    print(f'{len(checks)-failed}/{len(checks)} passed; {failed} failed')
    return failed


if __name__ == '__main__':
    raise SystemExit(bool(main()))
