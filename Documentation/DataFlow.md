# Передача данных в приложении

| Сценарий | Откуда отправляются данные | JSON-поля | PHP API | Таблицы базы данных | Что получает приложение |
|---|---|---|---|---|---|
| Регистрация клиента | `ClientRegister` | `name`, `phone`, `password` | `clients/register.php` | `vladelci` | Данные клиента для `State` |
| Вход клиента | `MainPage` | `phone`, `password` | `clients/login.php` | `vladelci` | `id`, `name`, `phone` для `State` |
| Личный кабинет | `ClientPage` | `phone` в адресе запроса | `clients/profile.php` | `vladelci`, `pets`, `zapic_na_priem`, `services` | Клиент и его записи `appointments` |
| Добавление питомца | `AddPetPage` | `phone`, `name`, `type`, `age`, `weight`, `last_vaccination_date` | `pets/create.php` | `pets` | Сообщение об успешном добавлении |
| Изменение питомца | `EditPetPage` | `id`, `phone`, `name`, `type`, `age`, `weight`, `last_vaccination_date` | `pets/update.php` | `pets` | Сообщение об успешном изменении |
| Удаление питомца | `PetsPage` | `id`, `phone` | `pets/delete.php` | `pets` | Сообщение об удалении или причине запрета |
| Загрузка питомцев | `PetsPage`, `RecordPage` | `phone` в адресе запроса | `pets/list.php` | `pets`, `vladelci` | Список `Pet` |
| Запись с новым питомцем | `RecordPage` | `pet_id = 0`, имя, вид, возраст, услуга, время, комментарий | `appointments/create.php` | `vladelci`, `pets`, `zapic_na_priem` | Новая запись на прием |
| Запись с сохраненным питомцем | `RecordPage` | `pet_id`, услуга, время, комментарий | `appointments/create.php` | `pets`, `zapic_na_priem` | Запись привязывается к выбранному питомцу |
| Отмена записи | `ClientPage` | `id`, `phone` | `appointments/cancel.php` | `zapic_na_priem` | Статус записи меняется на `cancelled` |
| Смена статуса | `RequestPage` | `id`, `status` | `appointments/update_status.php` | `zapic_na_priem` | Обновленный статус записи |
| Комментарий администратора | `RequestPage` | `id`, `admin_comment` | `appointments/update_admin_comment.php` | `zapic_na_priem` | Комментарий виден клиенту в кабинете |
| Медицинская запись | `MedicalRecordPage` | `id`, жалоба, диагноз, результат, лечение, процедура, заметки | `appointments/update_medical_record.php` | `result_obsled`, `lechenie` | Медицинские данные для заявки |
| Список услуг | `ServicePage`, `RecordPage` | Нет данных, обычный GET-запрос | `services/list.php` | `services` | Список услуг и цен |
| Добавление услуги | `AddPage` | `title`, `description`, `price`, `category` | `services/create.php` | `services` | Новая услуга в списке |
| Изменение услуги | `EditPage` | `id`, `title`, `description`, `price`, `category` | `services/update.php` | `services` | Измененная услуга |
| Удаление услуги | `ServicePage` | `id` | `services/delete.php` | `services` | Услуга физически удаляется из базы |
| Расписание клиента | `RecordPage` | Нет данных, обычный GET-запрос | `schedule/free_slots.php` | `zapic_na_priem` | Свободные даты и временные слоты |
| Расписание администратора | `AdminTimePage` | Нет данных, обычный GET-запрос | `schedule/free_slots.php`, `schedule/get_zapis_admin.php` | `zapic_na_priem`, `pets`, `vladelci`, `services` | Слоты с данными занятых записей |

## Состояние клиента

| Свойство `State` | Откуда заполняется | Где используется |
|---|---|---|
| `ClientId` | После регистрации или входа | Данные текущего клиента |
| `ClientName` | После регистрации или входа | Личный кабинет и форма записи |
| `ClientPhone` | После регистрации или входа | Все запросы клиента к API |
| `SelectedPet` | После нажатия «Изменить» в списке питомцев | Страница изменения питомца |
| `CurrentMedicalRecord` | После выбора медкарты администратором | Страница медицинской записи |
| `IsAdminMode` | После успешного входа администратора | Показ кнопок управления услугами |

## Как работает `pet_id` при записи

| Выбор в `SavedPetPicker` | `pet_id` | Поля питомца в форме | Действие API |
|---|---:|---|---|
| Новый питомец | `0` | Можно изменять | API ищет или создает питомца по введенным данным |
| Сохраненный питомец | ID питомца | Заблокированы | API проверяет владельца и использует точный ID питомца |
