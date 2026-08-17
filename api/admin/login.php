<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);

$login = trim($data["login"] ?? "");
$password = trim($data["password"] ?? "");

if ($login == "" || $password == "") {
    echo json_encode(["success" => false, "message" => "Введите логин и пароль"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "SELECT id, login, password, name FROM admins WHERE login = ? LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $login);
$stmt->execute();
$result = $stmt->get_result();
$admin = mysqli_fetch_assoc($result);

if (!$admin || !password_verify($password, $admin["password"])) {
    echo json_encode(["success" => false, "message" => "Неверный логин или пароль"], JSON_UNESCAPED_UNICODE);
    exit;
}

echo json_encode([
    "success" => true,
    "message" => "Вход выполнен",
    "user" => [
        "id" => (int)$admin["id"],
        "name" => $admin["name"],
        "role" => "admin"
    ]
], JSON_UNESCAPED_UNICODE);


