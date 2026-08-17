<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);

$phone = trim($data["phone"] ?? "");
$password = trim($data["password"] ?? "");

if ($phone == "" || $password == "") {
    echo json_encode(["success" => false, "message" => "Введите телефон и пароль"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "SELECT id, name, phone, password FROM vladelci WHERE phone = ? LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$result = $stmt->get_result();
$client = mysqli_fetch_assoc($result);

if (!$client || empty($client["password"]) || !password_verify($password, $client["password"])) {
    echo json_encode(["success" => false, "message" => "Неверный телефон или пароль"], JSON_UNESCAPED_UNICODE);
    exit;
}

echo json_encode([
    "success" => true,
    "message" => "Вход выполнен",
    "client" => [
        "id" => (int)$client["id"],
        "name" => $client["name"],
        "phone" => $client["phone"]
    ]
], JSON_UNESCAPED_UNICODE);


