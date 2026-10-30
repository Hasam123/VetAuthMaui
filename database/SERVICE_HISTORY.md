# Неизменяемые снимки оказанных услуг

Миграции: `migrations/20261030_history_snapshots.sql` и
`migrations/20261030_history_medical_snapshot.sql` (11 дополнительных полей, всего 27).

Оба триггера завершения приёма копируют значения в самостоятельную строку
`service_history`. В таблице нет внешних ключей, вычисляемых полей и обращений
к исходным таблицам при чтении сохранённой строки.

| Поле | Источник при создании строки |
|---|---|
| id | Собственный AUTO_INCREMENT истории |
| client_id | vladelci.id, копия числа |
| client_name | vladelci.name |
| pet_id | pets.id, копия числа |
| pet_name | pets.name |
| service_name | services.name |
| doctor_name | admins.name |
| data_created | CURRENT_TIMESTAMP, прежнее поведение |
| kol | 1, прежнее поведение |
| total_price | services.price |
| address | vladelci.address |
| pet_type | pets.type |
| pol | pets.pol |
| pet_age | pets.age |
| service_category | services.category |
| vid_priema | Первичный / Повторный по perv_priem_id на момент завершения |
| phone | vladelci.phone — телефон владельца |
| weight | pets.weight — вес питомца |
| data_zabol | medical_records.data_zabol — дата начала заболевания без времени |
| jaloba | medical_records.jaloba — жалобы |
| pred_diag | medical_records.pred_diag — предварительный диагноз |
| diagnoz | medical_records.diagnoz — заключительный диагноз |
| klin_prizn | medical_records.klin_prizn — клинические признаки и исследования |
| naz_lech | medical_records.naz_lech — назначенное лечение |
| procedure_done | medical_records.procedure_done — выполненные процедуры |
| ishod | medical_records.ishod — исход заболевания |
| notes | medical_records.notes — примечания врача |

Изменения владельца, питомца, услуги, врача, заявки или медкарты не обновляют историю.
Удаление исходных данных не удаляет снимки. Значения client_id и pet_id остаются
историческими числами даже после удаления исходных строк.

UPDATE и DELETE истории запрещены существующими триггерами. Повторное сохранение
карты не добавляет строку, пока статус приёма остаётся done. Запрет возврата
завершённого приёма в другой статус сохраняется.

Ранее сохранённые строки не переписываются. Новые поля в них остаются NULL:
текущие значения справочников нельзя считать достоверными историческими значениями.
Новая дата приёма и отдельный учёт нескольких услуг не добавлялись.

Медкарта читается через LEFT JOIN только при создании снимка. Если её нет,
медицинские поля сохраняются как NULL; последующее создание карты не меняет снимок.
При обычном завершении через API карта сохраняется перед переводом в done,
в одной транзакции с созданием истории.

Проверка на отдельной временной базе: `C:\xampp\php\php.exe tests\clinic_journal.php`.
