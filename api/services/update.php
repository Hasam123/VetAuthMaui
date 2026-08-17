<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);

$id = (int)($data["id"] ?? 0);
$title = trim($data["title"] ?? "");
$description = trim($data["description"] ?? "");
$price = (int)($data["price"] ?? 0);
$category = trim($data["category"] ?? "");

if ($id <= 0 || $title == "" || $description == "" || $price <= 0 || $category == "") {
    echo json_encode(["success" => false, "message" => "Заполните все поля"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "UPDATE services
                               SET name = ?, description = ?, price = ?, category = ?
                               WHERE id = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("ssisi", $title, $description, $price, $category, $id);
$stmt->execute();

echo json_encode(["success" => true, "message" => "Услуга обновлена"], JSON_UNESCAPED_UNICODE);


