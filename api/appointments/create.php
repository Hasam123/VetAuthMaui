<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);

$name = trim($data["name"] ?? "");
$phone = trim($data["phone"] ?? "");
$petName = trim($data["pet_name"] ?? "");
$petType = trim($data["pet_type"] ?? "");
$petAge = trim($data["pet_age"] ?? "");
$comment = trim($data["comment"] ?? "");
$petId = (int)($data["pet_id"] ?? 0);
$serviceId = (int)($data["service_id"] ?? 0);
$appointmentAt = trim($data["appointment_at"] ?? "");

if ($name == "" || $phone == "" || $petName == "" || $petType == "" || $serviceId <= 0 || $appointmentAt == "") {
    echo json_encode(["success" => false, "message" => "Заполните обязательные поля"], JSON_UNESCAPED_UNICODE);
    exit;
}

$time = strtotime($appointmentAt);

if (!$time) {
    echo json_encode(["success" => false, "message" => "Неверная дата записи"], JSON_UNESCAPED_UNICODE);
    exit;
}

$date = date("Y-m-d", $time);
$clock = date("H:i:s", $time);

$sql = "SELECT id
        FROM zapic_na_priem
        WHERE data = ? AND time = ? AND status IN ('new', 'accepted', 'done')";
$stmt = $conn->prepare($sql);
$stmt->bind_param("ss", $date, $clock);
$stmt->execute();
$busy = $stmt->get_result();

if (mysqli_num_rows($busy) > 0) {
    echo json_encode(["success" => false, "message" => "Это время уже занято"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "SELECT id
        FROM vladelci
        WHERE phone = ? AND password IS NOT NULL
        LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$result = $stmt->get_result();
$owner = mysqli_fetch_assoc($result);

if (!$owner) {
    echo json_encode([
        "success" => false,
        "message" => "Сначала зарегистрируйтесь и войдите в приложение"
    ], JSON_UNESCAPED_UNICODE);
    exit;
}

$ownerId = (int)$owner["id"];

if ($petId > 0) {
    $sql = "SELECT id FROM pets WHERE id = ? AND client_id = ?";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("ii", $petId, $ownerId);
    $stmt->execute();
    $pet = mysqli_fetch_assoc($stmt->get_result());

    if (!$pet) {
        echo json_encode(["success" => false, "message" => "Питомец не найден"], JSON_UNESCAPED_UNICODE);
        exit;
    }
} else {
    $sql = "SELECT id
            FROM pets
            WHERE client_id = ? AND type = ? AND name = ?
            LIMIT 1";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("iss", $ownerId, $petType, $petName);
    $stmt->execute();
    $result = $stmt->get_result();
    $pet = mysqli_fetch_assoc($result);

    if ($pet) {
        $petId = (int)$pet["id"];

        $sql = "UPDATE pets
                SET age = ?
                WHERE id = ?";
        $stmt = $conn->prepare($sql);
        $stmt->bind_param("si", $petAge, $petId);
        $stmt->execute();
    } else {
        $sql = "INSERT INTO pets (client_id, type, name, age)
                VALUES (?, ?, ?, ?)";
        $stmt = $conn->prepare($sql);
        $stmt->bind_param("isss", $ownerId, $petType, $petName, $petAge);
        $stmt->execute();
        $petId = mysqli_insert_id($conn);
    }
}

$status = "new";

$sql = "INSERT INTO zapic_na_priem (pet_id, service_id, data, time, status, comment)
                               VALUES (?, ?, ?, ?, ?, ?)";
$stmt = $conn->prepare($sql);
$stmt->bind_param("iissss", $petId, $serviceId, $date, $clock, $status, $comment);

try {
    $stmt->execute();
} catch (mysqli_sql_exception $e) {
    if ((int)$e->getCode() === 1062) {
        echo json_encode(["success" => false, "message" => "Это время уже занято"], JSON_UNESCAPED_UNICODE);
        exit;
    }

    error_log("Appointment creation failed: " . $e->getMessage());
    echo json_encode(["success" => false, "message" => "Не удалось создать запись"], JSON_UNESCAPED_UNICODE);
    exit;
}

echo json_encode(["success" => true, "message" => "Заявка отправлена"], JSON_UNESCAPED_UNICODE);


