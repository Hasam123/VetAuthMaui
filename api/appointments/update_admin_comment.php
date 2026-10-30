<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require __DIR__ . "/../../db.php";

$data = json_decode(file_get_contents("php://input"), true);
$id = (int)($data["id"] ?? 0);
$client_comment = trim($data["admin_comment"] ?? "");

if ($id <= 0) {
    echo json_encode(["success" => false, "message" => "Неверный ID заявки"], JSON_UNESCAPED_UNICODE);
    exit;
}

$stmt = $conn->prepare("SELECT id FROM zapic_na_priem WHERE id = ?");
$stmt->bind_param("i", $id);
$stmt->execute();
if ($stmt->get_result()->num_rows !== 1) {
    echo json_encode(["success" => false, "message" => "Заявка не найдена"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "UPDATE zapic_na_priem SET admin_comment = ? WHERE id = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("si", $client_comment, $id);
$stmt->execute();

echo json_encode(["success" => true, "message" => "Комментарий сохранен"], JSON_UNESCAPED_UNICODE);


