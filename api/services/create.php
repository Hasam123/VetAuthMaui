<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);

$title = trim($data["title"] ?? "");
$description = trim($data["description"] ?? "");
$price = (int)($data["price"] ?? 0);
$category = trim($data["category"] ?? "");

if ($title == "" || $description == "" || $price <= 0 || $category == "") {
    echo json_encode(["success" => false, "message" => "Заполните все поля"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "INSERT INTO services (name, description, price, category)
                               VALUES (?, ?, ?, ?)";
$stmt = $conn->prepare($sql);
$stmt->bind_param("ssis", $title, $description, $price, $category);
$stmt->execute();

echo json_encode([
    "success" => true,
    "message" => "Услуга добавлена",
    "id" => mysqli_insert_id($conn)
], JSON_UNESCAPED_UNICODE);


