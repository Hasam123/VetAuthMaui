<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);

$phone = trim($data["phone"] ?? "");
$name = trim($data["name"] ?? "");
$type = trim($data["type"] ?? "");
$age = trim($data["age"] ?? "");
$weight = trim($data["weight"] ?? "");
$lastVacData = trim($data["last_vac_data"] ?? "");

if ($phone == "" || $name == "" || $type == "") {
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

$sql = "SELECT id FROM vladelci WHERE phone = ? LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$result = $stmt->get_result();
$client = mysqli_fetch_assoc($result);

if (!$client) {
    echo json_encode(["success" => false, "message" => "Клиент не найден"], JSON_UNESCAPED_UNICODE);
    exit;
}

$clientId = (int)$client["id"];
$sql = "INSERT INTO pets (client_id, name, type, age, weight, last_vac_data)
        VALUES (?, ?, ?, ?, NULLIF(?, ''), ?)";
$stmt = $conn->prepare($sql);
$stmt->bind_param("isssss", $clientId, $name, $type, $age, $weight, $lastVacData);
$stmt->execute();

echo json_encode(["success" => true, "message" => "Питомец добавлен"], JSON_UNESCAPED_UNICODE);
