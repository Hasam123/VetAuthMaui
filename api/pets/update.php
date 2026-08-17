<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);
$id = (int)($data["id"] ?? 0);
$phone = trim($data["phone"] ?? "");
$name = trim($data["name"] ?? "");
$type = trim($data["type"] ?? "");
$age = trim($data["age"] ?? "");
$weight = trim($data["weight"] ?? "");
$lastVacData = trim($data["last_vac_data"] ?? "");

if ($id <= 0 || $phone == "" || $name == "" || $type == "") {
    echo json_encode(["success" => false, "message" => "Заполните кличку, вид животного и телефон"], JSON_UNESCAPED_UNICODE);
    exit;
}

if (mb_strlen($phone) > 15 || mb_strlen($age) > 15) {
    echo json_encode(["success" => false, "message" => "Телефон или возраст превышает допустимую длину"], JSON_UNESCAPED_UNICODE);
    exit;
}

if ($weight !== "") {
    $weight = str_replace(",", ".", $weight);
    if (!is_numeric($weight) || (float)$weight <= 0 || (float)$weight > 999.99) {
        echo json_encode(["success" => false, "message" => "Укажите вес от 0,01 до 999,99 кг"], JSON_UNESCAPED_UNICODE);
        exit;
    }
    $weight = number_format((float)$weight, 2, ".", "");
}

if ($lastVacData == "") {
    $lastVacData = null;
}

$sql = "UPDATE pets p
        INNER JOIN vladelci v ON v.id = p.client_id
        SET p.name = ?, p.type = ?, p.age = ?, p.weight = NULLIF(?, ''), p.last_vac_data = ?
        WHERE p.id = ? AND v.phone = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("sssssis", $name, $type, $age, $weight, $lastVacData, $id, $phone);
$stmt->execute();

if ($stmt->affected_rows === 0) {
    $check = $conn->prepare("SELECT p.id FROM pets p INNER JOIN vladelci v ON v.id = p.client_id WHERE p.id = ? AND v.phone = ?");
    $check->bind_param("is", $id, $phone);
    $check->execute();

    if (!$check->get_result()->fetch_assoc()) {
        echo json_encode(["success" => false, "message" => "Питомец не найден"], JSON_UNESCAPED_UNICODE);
        exit;
    }
}

echo json_encode(["success" => true, "message" => "Данные питомца изменены"], JSON_UNESCAPED_UNICODE);
