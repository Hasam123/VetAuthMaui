<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);
$id = (int)($data["id"] ?? 0);

if ($id <= 0) {
    echo json_encode(["success" => false, "message" => "Неверный ID услуги"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "DELETE FROM services WHERE id = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("i", $id);
$stmt->execute();

echo json_encode(["success" => true, "message" => "Услуга удалена"], JSON_UNESCAPED_UNICODE);


