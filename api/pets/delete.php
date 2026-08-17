<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);
$id = (int)($data["id"] ?? 0);
$phone = trim($data["phone"] ?? "");

if ($id <= 0 || $phone == "") {
    echo json_encode(["success" => false, "message" => "Не удалось определить питомца"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "SELECT z.id
        FROM zapic_na_priem z
        JOIN pets p ON p.id = z.pet_id
        JOIN vladelci v ON v.id = p.client_id
        WHERE p.id = ? AND v.phone = ?
        LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("is", $id, $phone);
$stmt->execute();

if ($stmt->get_result()->fetch_assoc()) {
    echo json_encode(["success" => false, "message" => "Нельзя удалить питомца с историей записей"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "DELETE p FROM pets p
        INNER JOIN vladelci v ON v.id = p.client_id
        WHERE p.id = ? AND v.phone = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("is", $id, $phone);
$stmt->execute();

if ($stmt->affected_rows === 0) {
    echo json_encode(["success" => false, "message" => "Питомец не найден"], JSON_UNESCAPED_UNICODE);
    exit;
}

echo json_encode(["success" => true, "message" => "Питомец удален"], JSON_UNESCAPED_UNICODE);
