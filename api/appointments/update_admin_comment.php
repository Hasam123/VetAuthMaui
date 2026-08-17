<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);
$id = (int)($data["id"] ?? 0);
$comment = trim($data["admin_comment"] ?? "");

if ($id <= 0) {
    echo json_encode(["success" => false, "message" => "Неверный ID заявки"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "UPDATE zapic_na_priem SET admin_comment = ? WHERE id = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("si", $comment, $id);
$stmt->execute();

echo json_encode(["success" => true, "message" => "Комментарий сохранен"], JSON_UNESCAPED_UNICODE);


