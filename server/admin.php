<?php
session_start();

require "db.php";

if (isset($_POST['enter'])) {
    $login = mysqli_real_escape_string($conn, trim($_POST['login'] ?? ""));
    $password = trim($_POST['password'] ?? "");
    $result = mysqli_query($conn, "SELECT id, password FROM admins WHERE login = '$login' LIMIT 1");
    $admin = mysqli_fetch_assoc($result);

    if ($admin && password_verify($password, $admin['password'])) {
        $_SESSION['admin'] = "yes";
    } else {
        echo "Неверный логин или пароль";
    }
}

if (isset($_GET['exit'])) {
    session_destroy();
    header("Location: admin.php");
    exit;
}

if (!isset($_SESSION['admin']) || $_SESSION['admin'] != "yes") {
?>

<!doctype html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <link rel="stylesheet" href="/veterenar/css/normalize.css">
    <link rel="stylesheet" href="/veterenar/css/style.css">
    <title>Авторизация</title>
</head>

<body>
<div class="login-form">
    <h2>Вход</h2>

    <form method="post">
        <input type="text" name="login" placeholder="Логин">
        <input type="password" name="password" placeholder="Пароль">
        <button type="submit" name="enter">Войти</button>
    </form>

    <a href="index.php" class="exit">На главную</a>
</div>
</body>
</html>

<?php
exit;
}

if (isset($_POST['add_service'])) {
    $title = mysqli_real_escape_string($conn, $_POST['title']);
    $opisanie = mysqli_real_escape_string($conn, $_POST['opisanie']);
    $price = (int)$_POST['price'];
    $category = mysqli_real_escape_string($conn, $_POST['category']);

    mysqli_query($conn, "INSERT INTO services (name, description, price, category)
                         VALUES ('$title', '$opisanie', '$price', '$category')");

    header("Location: admin.php");
    exit;
}

if (isset($_POST['update_service'])) {
    $id = (int)$_POST['id'];
    $title = mysqli_real_escape_string($conn, $_POST['title']);
    $opisanie = mysqli_real_escape_string($conn, $_POST['opisanie']);
    $price = (int)$_POST['price'];
    $category = mysqli_real_escape_string($conn, $_POST['category']);

    mysqli_query($conn, "UPDATE services
                         SET name = '$title',
                             description = '$opisanie',
                             price = '$price',
                             category = '$category'
                         WHERE id = $id");

    header("Location: admin.php");
    exit;
}

if (isset($_GET['delete_service'])) {
    $id = (int)$_GET['delete_service'];
    mysqli_query($conn, "DELETE FROM services WHERE id = $id");

    header("Location: admin.php");
    exit;
}

if (isset($_GET['delete_feedback'])) {
    $id = (int)$_GET['delete_feedback'];
    try {
        mysqli_query($conn, "DELETE FROM zapic_na_priem WHERE id = $id");
    } catch (mysqli_sql_exception $e) {
        if ((int)$e->getCode() !== 1451) { throw $e; }
        http_response_code(409);
        exit('Нельзя удалить приём с медицинской картой');
    }

    header("Location: admin.php");
    exit;
}

if (isset($_POST['update_feedback_status'])) {
    $id = (int)$_POST['id'];
    $status = mysqli_real_escape_string($conn, $_POST['status']);
    $allowedStatuses = ['new', 'accepted', 'done', 'cancelled'];

    if (in_array($status, $allowedStatuses)) {
        $stmt = $conn->prepare('SELECT status FROM zapic_na_priem WHERE id = ?');
        $stmt->bind_param('i', $id);
        $stmt->execute();
        $current = $stmt->get_result()->fetch_assoc();
        if (!$current || ($current['status'] === 'done' && $status !== 'done')) {
            http_response_code(400);
            exit('Недопустимый переход статуса');
        }
        if ($status === 'done') {
            $stmt = $conn->prepare("SELECT id FROM medical_records WHERE id = ? AND
                (COALESCE(jaloba,'') <> '' OR COALESCE(diagnoz,'') <> '' OR COALESCE(naz_lech,'') <> ''
                 OR COALESCE(procedure_done,'') <> '' OR COALESCE(notes,'') <> '')");
            $stmt->bind_param('i', $id);
            $stmt->execute();
            if ($current['status'] === 'cancelled' || !$stmt->get_result()->num_rows) {
                http_response_code(400);
                exit('Сначала заполните медицинскую карту действующего приёма');
            }
        }
        mysqli_query($conn, "UPDATE zapic_na_priem SET status = '$status' WHERE id = $id");
    }

    header("Location: admin.php");
    exit;
}

if (isset($_POST['update_admin_comment'])) {
    $id = (int)$_POST['id'];
    $adminComment = mysqli_real_escape_string($conn, $_POST['admin_comment']);

    mysqli_query($conn, "UPDATE zapic_na_priem SET admin_comment = '$adminComment' WHERE id = $id");

    header("Location: admin.php");
    exit;
}

if (isset($_POST['update_medical_record'])) {
    $id = (int)($_POST['id'] ?? 0);
    $jaloba = trim($_POST['jaloba'] ?? '');
    $diagnoz = trim($_POST['diagnoz'] ?? '');
    $nazLech = trim($_POST['naz_lech'] ?? '');
    $procedureDone = trim($_POST['procedure_done'] ?? '');
    $notes = trim($_POST['treatment_notes'] ?? '');
    if ($id <= 0 || ($jaloba === '' && $diagnoz === '' && $nazLech === '' && $procedureDone === '' && $notes === '')) {
        http_response_code(400);
        exit('Заполните медицинскую карту и выберите приём');
    }
    $conn->begin_transaction();
    try {
        $stmt = $conn->prepare('SELECT status FROM zapic_na_priem WHERE id = ? FOR UPDATE');
        $stmt->bind_param('i', $id);
        $stmt->execute();
        $appointment = $stmt->get_result()->fetch_assoc();
        if (!$appointment || $appointment['status'] === 'cancelled') {
            throw new RuntimeException('Приём не найден или отменён');
        }
        $stmt = $conn->prepare('INSERT INTO medical_records (id, jaloba, diagnoz, naz_lech, procedure_done, notes)
            VALUES (?, ?, ?, ?, ?, ?) ON DUPLICATE KEY UPDATE jaloba=VALUES(jaloba), diagnoz=VALUES(diagnoz),
            naz_lech=VALUES(naz_lech), procedure_done=VALUES(procedure_done), notes=VALUES(notes)');
        $stmt->bind_param('isssss', $id, $jaloba, $diagnoz, $nazLech, $procedureDone, $notes);
        $stmt->execute();
        $conn->commit();
    } catch (Throwable $e) {
        $conn->rollback();
        error_log('Web medical record save failed: ' . $e->getMessage());
        http_response_code(400);
        exit('Не удалось сохранить медицинскую карту');
    }
    header('Location: admin.php');
    exit;
}
$editService = false;

if (isset($_GET['edit_service'])) {
    $id = (int)$_GET['edit_service'];
    $editResult = mysqli_query($conn, "SELECT * FROM services WHERE id = $id");
    $editService = mysqli_fetch_assoc($editResult);
}

$services = mysqli_query($conn, "SELECT * FROM services ORDER BY id DESC");

$feedback = mysqli_query($conn, "SELECT z.id, z.doctor_id, doctor.name AS doctor_name, v.name, v.phone,
                                        p.name AS pet_name,
                                        p.type AS pet_type,
                                        p.age AS pet_age,
                                        s.name AS service_title,
                                        TIMESTAMP(z.data, z.time) AS appointment_at,
                                        z.client_comment, z.admin_comment,
                                        z.status, z.data_created AS data_created,
                                        m.jaloba, m.diagnoz,
                                        m.naz_lech, m.procedure_done, m.notes AS treatment_notes
                                 FROM zapic_na_priem z
                                 JOIN pets p ON p.id = z.pet_id
                                 LEFT JOIN admins doctor ON doctor.id = z.doctor_id
                                 JOIN vladelci v ON v.id = p.client_id
                                 LEFT JOIN services s ON s.id = z.service_id
                                 LEFT JOIN medical_records m ON m.id = z.id
                                 WHERE z.status IN ('new', 'accepted', 'done', 'cancelled')
                                 ORDER BY z.data_created DESC");
?>

<!doctype html>
<html lang="ru">

<head>
    <meta charset="UTF-8">
    <title>Админ-панель</title>
    <link rel="stylesheet" href="/veterenar/css/normalize.css">
    <link rel="stylesheet" href="/veterenar/css/style.css">
</head>

<body>

<div class="admin">
    <h1>Админ-панель</h1>

    <a href="admin.php?exit=1">Выйти</a>

    <br><br>

    <?php if ($editService) { ?>
        <h2>Редактировать услугу</h2>
    <?php } else { ?>
        <h2>Добавить услугу</h2>
    <?php } ?>

    <form method="post" class="admin-form">
        <?php if ($editService) { ?>
            <input type="hidden" name="id" value="<?php echo $editService['id']; ?>">
        <?php } ?>

        <input type="text" name="title" placeholder="Название услуги" required
               value="<?php if ($editService) echo $editService['name']; ?>">

        <textarea name="opisanie" placeholder="Описание услуги" required><?php if ($editService) echo $editService['description']; ?></textarea>

        <input type="number" name="price" placeholder="Цена" required
               value="<?php if ($editService) echo $editService['price']; ?>">

        <select name="category" required>
            <option value="">Выберите категорию</option>
            <option value="terapia" <?php if ($editService && $editService['category'] == "terapia") echo "selected"; ?>>Терапия</option>
            <option value="lab" <?php if ($editService && $editService['category'] == "lab") echo "selected"; ?>>Лаборатория</option>
            <option value="hirurgia" <?php if ($editService && $editService['category'] == "hirurgia") echo "selected"; ?>>Хирургия</option>
            <option value="vactinatia" <?php if ($editService && $editService['category'] == "vactinatia") echo "selected"; ?>>Вакцинация</option>
            <option value="stomatologia" <?php if ($editService && $editService['category'] == "stomatologia") echo "selected"; ?>>Стоматология</option>
            <option value="allergia" <?php if ($editService && $editService['category'] == "allergia") echo "selected"; ?>>Аллергология</option>
            <option value="other" <?php if ($editService && $editService['category'] == "other") echo "selected"; ?>>Прочее</option>
        </select>

        <?php if ($editService) { ?>
            <button type="submit" name="update_service">Сохранить изменения</button>
            <a href="admin.php" class="edit-btn">Отмена</a>
        <?php } else { ?>
            <button type="submit" name="add_service">Добавить услугу</button>
        <?php } ?>
    </form>

    <h2>Список услуг</h2>

    <table class="admin-table">
        <thead>
        <tr>
            <th>ID</th>
            <th>Название</th>
            <th>Описание</th>
            <th>Цена</th>
            <th>Категория</th>
            <th>Редактировать</th>
            <th>Удалить</th>
        </tr>
        </thead>

        <tbody>
        <?php while ($row = mysqli_fetch_assoc($services)) { ?>
            <tr>
                <td><?php echo $row['id']; ?></td>
                <td><?php echo $row['name']; ?></td>
                <td><?php echo $row['description']; ?></td>
                <td><?php echo $row['price']; ?> руб.</td>
                <td><?php echo $row['category']; ?></td>
                <td>
                    <a class="edit-btn" href="admin.php?edit_service=<?php echo $row['id']; ?>">Редактировать</a>
                </td>
                <td>
                    <a class="delete-btn" href="admin.php?delete_service=<?php echo $row['id']; ?>">Удалить</a>
                </td>
            </tr>
        <?php } ?>
        </tbody>
    </table>

    <h2>Заявки на прием</h2>

    <table class="admin-table">
        <thead>
        <tr>
            <th>ID</th>
            <th>Имя</th>
            <th>Телефон</th>
            <th>Питомец</th>
            <th>Вид</th>
            <th>Возраст</th>
            <th>Услуга</th>
            <th>Время записи</th>
            <th>Комментарий</th>
            <th>Комментарий администратора</th>
            <th>Обследование и лечение</th>
            <th>Статус</th>
            <th>Дата</th>
            <th>Удалить</th>
        </tr>
        </thead>

        <tbody>
        <?php while ($row = mysqli_fetch_assoc($feedback)) { ?>
            <tr>
                <td><?php echo $row['id']; ?></td>
                <td><?php echo $row['name']; ?></td>
                <td><?php echo $row['phone']; ?></td>
                <td><?php echo $row['pet_name']; ?></td>
                <td><?php echo $row['pet_type']; ?></td>
                <td><?php echo $row['pet_age']; ?></td>
                <td><?php echo $row['service_title']; ?><br>Врач: <?php echo htmlspecialchars($row['doctor_name'] ?? 'Не указан', ENT_QUOTES, 'UTF-8'); ?></td>
                <td><?php echo $row['appointment_at']; ?></td>
                <td><?php echo $row['client_comment']; ?></td>

                <td>
                    <form method="post">
                        <input type="hidden" name="id" value="<?php echo $row['id']; ?>">
                        <textarea name="admin_comment" rows="2"><?php echo $row['admin_comment']; ?></textarea>
                        <button type="submit" name="update_admin_comment">OK</button>
                    </form>
                </td>

                <td>
                    <form method="post" class="medical-form">
                        <input type="hidden" name="id" value="<?php echo $row['id']; ?>">
                        <textarea name="jaloba" rows="2" placeholder="Жалоба"><?php echo $row['jaloba']; ?></textarea>
                        <textarea name="diagnoz" rows="2" placeholder="Диагноз"><?php echo $row['diagnoz']; ?></textarea>
                        <textarea name="naz_lech" rows="2" placeholder="Назначенное лечение"><?php echo $row['naz_lech']; ?></textarea>
                        <textarea name="procedure_done" rows="2" placeholder="Что сделали"><?php echo $row['procedure_done']; ?></textarea>
                        <textarea name="treatment_notes" rows="2" placeholder="Заметки"><?php echo $row['treatment_notes']; ?></textarea>
                        <button type="submit" name="update_medical_record">Сохранить</button>
                    </form>
                </td>

                <td>
                    <form method="post">
                        <input type="hidden" name="id" value="<?php echo $row['id']; ?>">
                        <select name="status">
                            <option value="new" <?php if ($row['status'] == "new") echo "selected"; ?>>Новая</option>
                            <option value="accepted" <?php if ($row['status'] == "accepted") echo "selected"; ?>>Принята</option>
                            <option value="done" <?php if ($row['status'] == "done") echo "selected"; ?>>Выполнена</option>
                            <option value="cancelled" <?php if ($row['status'] == "cancelled") echo "selected"; ?>>Отменена</option>
                        </select>
                        <button type="submit" name="update_feedback_status">OK</button>
                    </form>
                </td>

                <td><?php echo $row['data_created']; ?></td>

                <td>
                    <a class="delete-btn" href="admin.php?delete_feedback=<?php echo $row['id']; ?>">Удалить</a>
                </td>
            </tr>
        <?php } ?>
        </tbody>
    </table>
</div>

</body>
</html>

