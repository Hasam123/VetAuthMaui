<?php
// Run: C:\xampp\php\php.exe tests/medical_records_cycle.php
// Only creates/deletes a randomly named vet_medtest_* database. Production is read-only.
require __DIR__ . '/../scripts/Migrate-MedicalRecords.php';
require __DIR__ . '/../scripts/Migrate-FieldNames.php';
$root = dirname(__DIR__);
$source = 'vet_clinic';
$name = 'vet_medtest_' . bin2hex(random_bytes(6));
$temp = sys_get_temp_dir() . DIRECTORY_SEPARATOR . $name;
$db = new mysqli('localhost', 'root', '');
$db->set_charset('utf8mb4');
$passed = 0; $failed = 0; $checks = []; $server = null;
function check(bool $ok, string $label): void {
    global $passed, $failed, $checks;
    $checks[] = ['check' => $label, 'passed' => $ok];
    if ($ok) { $passed++; } else { $failed++; }
    echo ($ok ? 'PASS ' : 'FAIL ') . $label . PHP_EOL;
}
function scalar(string $sql) { global $db; return $db->query($sql)->fetch_row()[0]; }
function request(string $path, ?array $data = null): array {
    global $base;
    $context = stream_context_create(['http' => ['method' => $data === null ? 'GET' : 'POST',
        'header' => "Content-Type: application/json\r\n", 'ignore_errors' => true, 'timeout' => 10,
        'content' => $data === null ? '' : json_encode($data, JSON_UNESCAPED_UNICODE)]]);
    $body = file_get_contents($base . '/api/' . $path, false, $context);
    $value = json_decode($body, true);
    if (!is_array($value)) { throw new RuntimeException("Invalid API JSON: $path: " . substr($body, 0, 400)); }
    return $value;
}
function saveRecord(int $id, array $fields): array { return request('appointments/update_medical_record.php', ['id' => $id] + $fields); }
function copyDirectory(string $from, string $to): void {
    mkdir($to, 0777, true);
    foreach (new DirectoryIterator($from) as $f) {
        if ($f->isDot()) { continue; }
        $destination = $to . DIRECTORY_SEPARATOR . $f->getFilename();
        if ($f->isDir()) { copyDirectory($f->getPathname(), $destination); }
        else { copy($f->getPathname(), $destination); }
    }
}
function removeDirectory(string $path): void {
    // Called only for the exact randomly generated test directory.
    if (!is_dir($path)) { return; }
    foreach (new DirectoryIterator($path) as $f) {
        if ($f->isDot()) { continue; }
        if ($f->isDir()) { removeDirectory($f->getPathname()); } else { unlink($f->getPathname()); }
    }
    rmdir($path);
}
try {
    $sourceHashes=[];
    foreach(['admins','vladelci','pets','services','zapic_na_priem','medical_records','service_history'] as $table) {
        if($db->query("SHOW TABLES FROM `$source` LIKE '$table'")->num_rows) {
            $sourceHashes[$table]=hash('sha256',serialize($db->query("SELECT * FROM `$source`.`$table` ORDER BY id")->fetch_all(MYSQLI_ASSOC)));
        }
    }
    $adminBefore = $db->query("SELECT * FROM `$source`.admins ORDER BY id")->fetch_all(MYSQLI_ASSOC);
    $db->query("CREATE DATABASE `$name` CHARACTER SET utf8mb4");
    $db->select_db($name);
    $db->query('SET FOREIGN_KEY_CHECKS=0');
    foreach ($db->query("SHOW FULL TABLES FROM `$source` WHERE Table_type = 'BASE TABLE'")->fetch_all() as $row) {
        $table = $row[0];
        $ddl = $db->query("SHOW CREATE TABLE `$source`.`$table`")->fetch_row()[1];
        $db->query($ddl);
        $columns = $db->query("SHOW COLUMNS FROM `$table`")->fetch_all(MYSQLI_ASSOC);
        $columns = array_filter($columns, fn($c) => stripos($c['Extra'], 'GENERATED') === false);
        $fields = implode(',', array_map(fn($c) => '`' . $c['Field'] . '`', $columns));
        $db->query("INSERT INTO `$table` ($fields) SELECT $fields FROM `$source`.`$table`");
    }
    $db->query('SET FOREIGN_KEY_CHECKS=1');
    foreach ($db->query("SHOW TRIGGERS FROM `$source`")->fetch_all(MYSQLI_ASSOC) as $trigger) {
        $create = $db->query("SHOW CREATE TRIGGER `$source`.`{$trigger['Trigger']}`")->fetch_assoc();
        $db->query($create['SQL Original Statement']);
    }
    if (!in_array('--deployed', $argv, true)) { migrateFieldNames($db); }
    $alreadyMigrated = (bool)$db->query("SHOW TABLES LIKE 'medical_records'")->num_rows;
    if (!$alreadyMigrated) {
    $oldExam = $db->query('SELECT * FROM result_obsled ORDER BY id')->fetch_all(MYSQLI_ASSOC);
    $oldTreatment = $db->query('SELECT * FROM lechenie ORDER BY id')->fetch_all(MYSQLI_ASSOC);
    $historyCount = (int)scalar('SELECT COUNT(*) FROM service_history');
    $migration = migrateMedicalRecords($db);
    $expectedIds = array_unique(array_merge(array_column($oldExam,'zapic_id'), array_filter(array_column($oldTreatment,'zapic_id'),fn($id)=>$id!==null)));
    check($migration['medical_records'] === count($expectedIds), 'All existing appointment records migrated');
    check($migration['archived_unlinked_treatments'] === count(array_filter($oldTreatment,fn($r)=>$r['zapic_id']===null)), 'Unlinked treatment archived');
    check($oldExam === $db->query('SELECT * FROM result_obsled_legacy ORDER BY id')->fetch_all(MYSQLI_ASSOC), 'Original examinations preserved exactly');
    check($oldTreatment === $db->query('SELECT * FROM lechenie_legacy ORDER BY id')->fetch_all(MYSQLI_ASSOC), 'Original treatments preserved exactly');
    foreach ($oldExam as $r) {
        $m = $db->query('SELECT * FROM medical_records WHERE id=' . (int)$r['zapic_id'])->fetch_assoc();
        check($m && $m['jaloba'] === $r['jaloba'] && $m['diagnoz'] === $r['diagnoz']
            && (!$r['result'] || str_contains($m['notes'], $r['result'])), 'Migrated examination ' . $r['id']);
    }
    foreach ($oldTreatment as $r) {
        if ($r['zapic_id'] === null) { continue; }
        $m = $db->query('SELECT * FROM medical_records WHERE id=' . (int)$r['zapic_id'])->fetch_assoc();
        check($m && $m['naz_lech'] === $r['naz_lech'] && $m['procedure_done'] === $r['procedure_done']
            && (!$r['notes'] || str_contains($m['notes'], $r['notes'])), 'Migrated treatment ' . $r['id']);
    }
    check((int)scalar('SELECT COUNT(*) FROM service_history') === $historyCount, 'Migration does not backfill historical service prices');
    try { migrateMedicalRecords($db); check(false, 'Second migration rejected'); }
    catch (RuntimeException $e) { check(true, 'Second migration rejected'); }
    } else {
        check(true, 'Testing existing migrated database schema');
    }
    $historyColumns=array_column($db->query("SHOW COLUMNS FROM service_history")->fetch_all(MYSQLI_ASSOC),"Field");
    check(!in_array("appointment_id",$historyColumns) && !in_array("service_id",$historyColumns), "History no longer contains removed IDs");
    $columns = array_column($db->query('SHOW COLUMNS FROM medical_records')->fetch_all(MYSQLI_ASSOC), 'Field');
    check($columns === ['id','jaloba','diagnoz','naz_lech','procedure_done','notes','data_created'], 'Exact agreed seven-column schema');
    check(!str_contains($db->query('SHOW CREATE TABLE medical_records')->fetch_row()[1], 'AUTO_INCREMENT'), 'Medical record ID is appointment ID, no auto increment');
    try { $db->query("INSERT INTO medical_records(id,diagnoz) VALUES (2147483647,'invalid')"); check(false, 'Foreign key blocks missing appointment'); }
    catch (mysqli_sql_exception $e) { check($e->getCode() === 1452, 'Foreign key blocks missing appointment'); }

    $deployed = in_array('--deployed', $argv, true);
    $apiRoot = $deployed ? 'C:/xampp/htdocs/veterenar/api' : $root . '/api';
    copyDirectory($apiRoot, $temp . '/api');
    if ($deployed) { copyDirectory('C:/xampp/htdocs/veterenar/includes', $temp . '/includes'); }
    $connection = '<?php mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT); $conn=new mysqli("localhost","root","","' . $name . '"); $conn->set_charset("utf8mb4");';
    file_put_contents($temp . '/api/db.php', $connection);
    file_put_contents($temp . '/db.php', $connection);
    copy($deployed ? 'C:/xampp/htdocs/veterenar/admin.php' : $root . '/server/admin.php', $temp . '/admin.php');
    $socket = stream_socket_server('tcp://127.0.0.1:0', $errno, $errstr);
    $address = stream_socket_get_name($socket, false); fclose($socket);
    $base = 'http://' . $address;
    $server = proc_open([PHP_BINARY, '-d', 'session.save_path='.$temp, '-S', $address, '-t', $temp], [0=>['pipe','r'],1=>['file',$temp.'/server.log','a'],2=>['file',$temp.'/server.log','a']], $pipes, $temp);
    if (!is_resource($server)) { throw new RuntimeException('Cannot start test server'); }
    for ($i=0; $i<50; $i++) {
        $probe = @fsockopen('127.0.0.1', (int)substr(strrchr($address, ':'),1));
        if ($probe) { fclose($probe); break; } usleep(100000);
    }
    $phone = 'T' . substr(bin2hex(random_bytes(6)),0,12);
    check(request('clients/register.php', ['name'=>'Тестовый клиент','phone'=>$phone,'password'=>'test-pass'])['success'], 'Register client');
    check(request('clients/login.php', ['phone'=>$phone,'password'=>'test-pass'])['success'], 'Client login');
    check(!request('clients/login.php', ['phone'=>$phone,'password'=>'wrong'])['success'], 'Wrong password rejected');
    check(request('pets/create.php', ['phone'=>$phone,'name'=>'Барсик','type'=>'Кот','age'=>'2 года'])['success'], 'Create test pet');
    $owner = (int)scalar("SELECT id FROM vladelci WHERE phone='$phone'");
    $pet = (int)scalar("SELECT id FROM pets WHERE client_id=$owner");
    $doctor = (int)scalar('SELECT id FROM admins WHERE is_active=1 ORDER BY id LIMIT 1');
    $db->query("INSERT INTO services(name,category,description,price) VALUES ('Тестовый осмотр','Тест','Только в тестовой базе',250)");
    $service = (int)$db->insert_id;
    $slots = request('schedule/free_slots.php?doctor_id=' . $doctor);
    $available = [];
    foreach ($slots['days'] as $day) { foreach ($day['slots'] as $slot) { if ($slot['is_available']) { $available[]=$slot['time']; } } }
    check(count($available)>=12, 'Available schedule returned');
    $booking = ['name'=>'Тестовый клиент','phone'=>$phone,'pet_name'=>'Барсик','pet_type'=>'Кот','pet_age'=>'2 года',
        'pet_id'=>$pet,'doctor_id'=>$doctor,'service_id'=>$service,'appointment_at'=>$available[0],'client_comment'=>'Тест'];
    $book = function(int $slot) use ($booking,$available): int {
        $data = array_replace($booking,['appointment_at'=>$available[$slot]]);
        $r = request('appointments/create.php',$data);
        if (!$r['success']) { throw new RuntimeException('Test booking failed: ' . json_encode($r)); }
        return (int)scalar('SELECT MAX(id) FROM zapic_na_priem');
    };
    $id = $book(0); check($id>0, 'Create appointment through API');
    check(!request('appointments/create.php',$booking)['success'], 'Duplicate slot rejected');
    check(!request('appointments/update_status.php',['id'=>$id,'status'=>'done'])['success'], 'Cannot complete without medical record');
    check(!saveRecord($id,[])['success'], 'Empty record rejected');
    check(!saveRecord($id,['jaloba'=>'  ', 'diagnoz'=>"\n"])['success'], 'Whitespace-only record rejected');
    check(!saveRecord(2147483647,['diagnoz'=>'x'])['success'], 'Missing appointment rejected');
    check(!saveRecord(0,['diagnoz'=>'x'])['success'], 'Invalid appointment ID rejected');
    check(request('appointments/update_status.php',['id'=>$id,'status'=>'accepted'])['success'], 'Accept appointment');
    $fields = ['jaloba'=>"Не ест 🐱 ' OR 1=1 --",'diagnoz'=>'Тестовый диагноз','naz_lech'=>'Препарат 1 мл',
        'procedure_done'=>'Осмотр','treatment_notes'=>"Строка 1\nСтрока 2"];
    check(saveRecord($id,$fields)['success'], 'Save full medical record');
    check(scalar("SELECT status FROM zapic_na_priem WHERE id=$id")==='done', 'Saving completes appointment');
    $saved = $db->query("SELECT * FROM medical_records WHERE id=$id")->fetch_assoc();
    check($saved['jaloba']===$fields['jaloba'] && $saved['notes']===$fields['treatment_notes'], 'Unicode, apostrophes and newlines round trip');
    check(!empty($saved['data_created']), 'New record has creation timestamp');
    $historyId=(int)scalar("SELECT MAX(id) FROM service_history");
    $history = $db->query("SELECT * FROM service_history WHERE id=$historyId")->fetch_assoc();
    check($history && $history['doctor_name']===scalar("SELECT name FROM admins WHERE id=$doctor"), 'History includes doctor snapshot');
    $views = ['appointments/list.php?doctor_id='.$doctor,'clients/profile.php?phone='.$phone,'schedule/get_zapis_admin.php?doctor_id='.$doctor];
    foreach ($views as $path) {
        $r=request($path); $found=array_values(array_filter($r['requests'],fn($a)=>(int)$a['id']===$id));
        check($r['success'] && count($found)===1 && $found[0]['client_comment']===$booking['client_comment'] && !empty($found[0]['data_created']) && $found[0]['diagnoz']===$fields['diagnoz'] && $found[0]['naz_lech']===$fields['naz_lech']
            && $found[0]['treatment_notes']===$fields['treatment_notes'] && !array_key_exists('obsled_result',$found[0]), 'Read unified card: '.$path);
    }
    $historyRowsBeforeRepeat=(int)scalar('SELECT COUNT(*) FROM service_history');
    check(saveRecord($id,array_replace($fields,['diagnoz'=>'Уточнённый диагноз']))['success'], 'Repeated save updates same card');
    check((int)scalar('SELECT COUNT(*) FROM service_history')===$historyRowsBeforeRepeat, 'Repeated save does not append duplicate history');
    try { $db->query("UPDATE zapic_na_priem SET status='new' WHERE id=$id"); check(false,'Database blocks reopening completed appointment'); }
    catch (mysqli_sql_exception $e) { check($e->getCode()===1644,'Database blocks reopening completed appointment'); }
    check((int)scalar("SELECT COUNT(*) FROM medical_records WHERE id=$id")===1, 'No duplicate medical records');
    check(scalar("SELECT data_created FROM medical_records WHERE id=$id")===$saved['data_created'], 'Update preserves creation timestamp');
    check($history===$db->query("SELECT * FROM service_history WHERE id=$historyId")->fetch_assoc(), 'Repeated save leaves service history unchanged');
    check(!request('appointments/delete.php',['id'=>$id])['success'], 'API prevents deleting appointment with card');
    check((int)scalar("SELECT COUNT(*) FROM medical_records WHERE id=$id")===1, 'Rejected deletion preserves medical card');
    check(!request('appointments/cancel.php',['id'=>$id,'phone'=>$phone])['success'], 'Completed appointment cannot be cancelled');
    check(!request('appointments/update_status.php',['id'=>$id,'status'=>'new'])['success'], 'Completed appointment cannot be reopened');

    $cancel=$book(1);
    check(request('appointments/cancel.php',['id'=>$cancel,'phone'=>$phone])['success'], 'Cancel new appointment');
    check(!saveRecord($cancel,$fields)['success'], 'Cancelled appointment rejects medical save');
    check((int)scalar("SELECT COUNT(*) FROM medical_records WHERE id=$cancel")===0, 'Cancelled appointment has no partial card');
    $replacement=$book(1); check($replacement!==$cancel, 'Cancelled slot can be booked again');
    check(request('appointments/delete.php',['id'=>$replacement])['success'], 'Delete appointment without card');
    check(!request('appointments/delete.php',['id'=>$replacement])['success'], 'Repeated deletion returns error');

    // Failure after record insert: status trigger must roll the entire save back.
    $rollback=$book(2);
    $historyCountBeforeFailure=(int)scalar("SELECT COUNT(*) FROM service_history");
    $db->query("CREATE TRIGGER test_fail_status BEFORE UPDATE ON zapic_na_priem FOR EACH ROW BEGIN IF NEW.id=$rollback THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected status failure'; END IF; END");
    check(!saveRecord($rollback,$fields)['success'], 'Injected status failure reported');
    check((int)scalar("SELECT COUNT(*) FROM medical_records WHERE id=$rollback")===0, 'New medical insert rolled back');
    check(scalar("SELECT status FROM zapic_na_priem WHERE id=$rollback")==='new', 'Failed save preserves appointment status');
    check((int)scalar("SELECT COUNT(*) FROM service_history")===$historyCountBeforeFailure, 'Failed save does not append history');
    $db->query('DROP TRIGGER test_fail_status');
    check(saveRecord($rollback,$fields)['success'], 'Save works after fault removed');
    $original=$db->query("SELECT * FROM medical_records WHERE id=$rollback")->fetch_assoc();
    $db->query("CREATE TRIGGER test_fail_status BEFORE UPDATE ON zapic_na_priem FOR EACH ROW BEGIN IF NEW.id=$rollback THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected update failure'; END IF; END");
    check(!saveRecord($rollback,['diagnoz'=>'Must roll back'])['success'], 'Injected failure during existing card update reported');
    check($original===$db->query("SELECT * FROM medical_records WHERE id=$rollback")->fetch_assoc(), 'Existing medical card fully rolled back');
    $db->query('DROP TRIGGER test_fail_status');
    $historyFail=$book(3);
    $db->query("CREATE TRIGGER test_fail_history BEFORE INSERT ON service_history FOR EACH ROW BEGIN IF NEW.pet_id=$pet THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected history failure'; END IF; END");
    check(!saveRecord($historyFail,$fields)['success'], 'History trigger failure reported');
    check((int)scalar("SELECT COUNT(*) FROM medical_records WHERE id=$historyFail")===0 && scalar("SELECT status FROM zapic_na_priem WHERE id=$historyFail")==='new', 'History failure rolls back card and status');
    $db->query('DROP TRIGGER test_fail_history');
    foreach (['jaloba','diagnoz','naz_lech','procedure_done','treatment_notes'] as $i=>$field) {
        $single=$book(4+$i);
        check(saveRecord($single,[$field=>'Только '.$field])['success'], 'Single populated field accepted: '.$field);
    }
    foreach (["UPDATE service_history SET total_price=0 WHERE id=$historyId", "DELETE FROM service_history WHERE id=$historyId"] as $sql) {
        try { $db->query($sql); check(false,'Service history immutable'); }
        catch (mysqli_sql_exception $e) { check($e->getCode()===1644,'Service history immutable'); }
    }
    $otherDoctor=(int)scalar("SELECT id FROM admins WHERE id<>$doctor ORDER BY id LIMIT 1");
    $otherBefore=(int)scalar("SELECT is_active FROM admins WHERE id=$otherDoctor");
    $db->query("UPDATE admins SET is_active=1 WHERE id=$otherDoctor");
    $otherBooking=array_replace($booking,['doctor_id'=>$otherDoctor]);
    check(request('appointments/create.php',$otherBooking)['success'], 'Two doctors can receive appointments at same time');
    $otherId=(int)scalar('SELECT MAX(id) FROM zapic_na_priem');
    $otherList=request('appointments/list.php?doctor_id='.$otherDoctor);
    check(!array_filter($otherList['requests'],fn($r)=>(int)$r['doctor_id']!==$otherDoctor), 'Doctor filter excludes other doctors appointments');
    check(!array_filter($otherList['requests'],fn($r)=>(int)$r['id']===$id), 'First doctor card absent from second doctor list');
    $db->query("UPDATE admins SET is_active=0 WHERE id=$otherDoctor");
    $doctorList=request('doctors/list.php');
    check(!array_filter($doctorList['doctors'],fn($r)=>(int)$r['id']===$otherDoctor), 'Inactive doctor hidden in booking list');
    check(!request('appointments/create.php',array_replace($otherBooking,['appointment_at'=>$available[11]]))['success'], 'Direct booking of inactive doctor rejected');
    $inactiveSlots=request('schedule/free_slots.php?doctor_id='.$otherDoctor);
    $anyAvailable=false;
    foreach($inactiveSlots['days'] as $day) { foreach($day['slots'] as $s) { $anyAvailable=$anyAvailable || $s['is_available']; } }
    check(!$anyAvailable, 'Inactive doctor has no available slots');
    check((int)scalar("SELECT COUNT(*) FROM zapic_na_priem WHERE id=$otherId")===1, 'Deactivating doctor preserves existing appointments');
    $db->query("UPDATE admins SET is_active=$otherBefore WHERE id=$otherDoctor");
    $historyBefore=$db->query("SELECT * FROM service_history WHERE id=$historyId")->fetch_assoc();
    $db->query("UPDATE services SET price=9999,name='Changed service' WHERE id=$service");
    check($historyBefore===$db->query("SELECT * FROM service_history WHERE id=$historyId")->fetch_assoc(), 'Changing service price and name leaves history unchanged');
    $otherPhone='U'.substr(bin2hex(random_bytes(6)),0,12);
    check(request('clients/register.php',['name'=>'Другой клиент','phone'=>$otherPhone,'password'=>'test-pass'])['success'], 'Register second client');
    $otherProfile=request('clients/profile.php?phone='.$otherPhone);
    check($otherProfile['requests']===[], 'Second client profile excludes first client appointments');
    check(!request('appointments/create.php',array_replace($booking,['phone'=>$otherPhone,'appointment_at'=>$available[11]]))['success'], 'Cannot book another client pet');
    check(!request('appointments/cancel.php',['id'=>$otherId,'phone'=>$otherPhone])['success'], 'Cannot cancel another client appointment');
    // Test the separate web panel with a temporary password in the cloned DB only.
    $testPassword = 'web-test-password';
    $testHash = password_hash($testPassword, PASSWORD_DEFAULT);
    $doctorRow = $db->query("SELECT * FROM admins WHERE id=$doctor")->fetch_assoc();
    $stmt = $db->prepare('UPDATE admins SET password=? WHERE id=?');
    $stmt->bind_param('si',$testHash,$doctor); $stmt->execute();
    $cookie = '';
    $web = function(array $form=[], string $query='') use (&$cookie): array {
        global $base;
        $options=['method'=>$form?'POST':'GET','ignore_errors'=>true,'follow_location'=>0,
            'header'=>"Content-Type: application/x-www-form-urlencoded\r\nCookie: $cookie\r\n",
            'content'=>http_build_query($form),'timeout'=>10];
        $body=file_get_contents($base.'/admin.php'.$query,false,stream_context_create(['http'=>$options]));
        foreach ($http_response_header as $header) {
            if (preg_match('/^Set-Cookie: (PHPSESSID=[^;]+)/i',$header,$match)) { $cookie=$match[1]; }
        }
        preg_match('/\s(\d{3})\s/',$http_response_header[0],$status);
        return ['status'=>(int)$status[1],'body'=>$body];
    };
    $r=$web(['enter'=>'1','login'=>$doctorRow['login'],'password'=>$testPassword]);
    check($r['status']===200 && str_contains($r['body'],'medical-form'), 'Web doctor login and listing work after migration');
    check(!str_contains($r['body'],'name="obsled_result"'), 'Removed examination result field absent from web form');
    $webId=$book(9);
    $form=['update_medical_record'=>'1','id'=>$webId]+$fields;
    $r=$web($form);
    check($r['status']===302 && scalar("SELECT diagnoz FROM medical_records WHERE id=$webId")===$fields['diagnoz'], 'Web panel saves unified medical card');
    $read=request('clients/profile.php?phone='.$phone);
    $found=array_values(array_filter($read['requests'],fn($r)=>(int)$r['id']===$webId));
    check(count($found)===1 && $found[0]['treatment_notes']===$fields['treatment_notes'], 'Web-created card visible through client API');
    $webHistoryBefore=(int)scalar("SELECT COUNT(*) FROM service_history");
    $r=$web(['update_feedback_status'=>'1','id'=>$webId,'status'=>'done']);
    check($r['status']===302 && (int)scalar("SELECT COUNT(*) FROM service_history")===$webHistoryBefore+1, 'Web completion records service history');
    $r=$web([], '?delete_feedback='.$webId);
    check($r['status']===409 && (int)scalar("SELECT COUNT(*) FROM medical_records WHERE id=$webId")===1, 'Web deletion protects medical card');
    $r=$web(['update_medical_record'=>'1','id'=>$webId,'jaloba'=>' ']);
    check($r['status']===400, 'Web rejects empty medical card');
    $r=$web(['update_medical_record'=>'1','id'=>$cancel]+$fields);
    check($r['status']===400, 'Web rejects cancelled appointment card');
    $r=$web(['update_feedback_status'=>'1','id'=>$webId,'status'=>'new']);
    check($r['status']===400 && scalar("SELECT status FROM zapic_na_priem WHERE id=$webId")==='done', 'Web rejects reopening completed appointment');
    $blank=$book(10);
    $r=$web(['update_feedback_status'=>'1','id'=>$blank,'status'=>'done']);
    check($r['status']===400 && scalar("SELECT status FROM zapic_na_priem WHERE id=$blank")==='new', 'Web rejects completion without card');
    $originalHash=$doctorRow['password'];
    $stmt=$db->prepare('UPDATE admins SET password=? WHERE id=?');
    $stmt->bind_param('si',$originalHash,$doctor); $stmt->execute();
    check($adminBefore===$db->query('SELECT * FROM admins ORDER BY id')->fetch_all(MYSQLI_ASSOC), 'Cloned admin data restored after web test');
    if (!$alreadyMigrated) {
    check($oldExam===$db->query('SELECT * FROM result_obsled_legacy ORDER BY id')->fetch_all(MYSQLI_ASSOC)
        && $oldTreatment===$db->query('SELECT * FROM lechenie_legacy ORDER BY id')->fetch_all(MYSQLI_ASSOC), 'API never writes legacy tables');
    }
    check($adminBefore===$db->query("SELECT * FROM `$source`.admins ORDER BY id")->fetch_all(MYSQLI_ASSOC), 'Production admins untouched');
    $historyFields=array_column($db->query('SHOW COLUMNS FROM service_history')->fetch_all(MYSQLI_ASSOC),'Field');
    check($historyFields===['id','client_id','client_name','pet_id','pet_name','service_name','doctor_name','data_created','kol','total_price'], 'Exact current history schema including kol and data_created');
    check((int)$history['kol']===1 && !empty($history['data_created']), 'History trigger populates kol=1 and timestamp');
    $baseInsert="INSERT INTO service_history(client_id,client_name,pet_id,pet_name,service_name,data_created,kol,total_price) VALUES ($owner,'Test',$pet,'Test','Test',NOW(),";
    foreach([['0,1)', 'Zero quantity rejected'],['-1,1)', 'Negative quantity rejected'],['1,-1)', 'Negative total price rejected']] as [$values,$label]) {
        try { $db->query($baseInsert.$values); check(false,$label); }
        catch(mysqli_sql_exception $e) { check($e->getCode()===4025,$label); }
    }
    $db->query("INSERT INTO service_history(client_id,client_name,pet_id,pet_name,service_name,data_created,total_price) VALUES ($owner,'Test',$pet,'Test','Default test',NOW(),0)");
    check((int)scalar('SELECT kol FROM service_history ORDER BY id DESC LIMIT 1')===1, 'Quantity defaults to one when omitted');
    foreach($sourceHashes as $table=>$before) {
        check($before===hash('sha256',serialize($db->query("SELECT * FROM `$source`.`$table` ORDER BY id")->fetch_all(MYSQLI_ASSOC))), 'Production data unchanged: '.$table);
    }
} catch (Throwable $e) {
    check(false, 'Unexpected error: '.$e->getMessage());
} finally {
    if (is_resource($server)) { proc_terminate($server); proc_close($server); }
    if (preg_match('/^vet_medtest_[a-f0-9]{12}$/D',$name)) { $db->query("DROP DATABASE IF EXISTS `$name`"); }
    if (is_dir($temp)) { removeDirectory($temp); }
    echo "RESULT $passed passed; $failed failed" . PHP_EOL;
    file_put_contents(__DIR__.'/medical_records_results.json',json_encode(['passed'=>$passed,'failed'=>$failed,'checks'=>$checks],JSON_UNESCAPED_UNICODE|JSON_PRETTY_PRINT));
}
exit($failed ? 1 : 0);
