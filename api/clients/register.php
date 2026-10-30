<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require __DIR__ . "/../../db.php";

$data = json_decode(file_get_contents("php://input"), true);

$name = trim($data["name"] ?? "");
$phone = trim($data["phone"] ?? "");
$password = trim($data["password"] ?? "");
$address = trim($data['address'] ?? '');

if ($name == "" || $phone == "" || $password == "" || $address == "") {
    echo json_encode(["success" => false, "message" => "Заполните ФИО, телефон, адрес и пароль"], JSON_UNESCAPED_UNICODE);
    exit;
}

if (mb_strlen($phone) > 15) {
    echo json_encode(["success" => false, "message" => "Телефон должен содержать не более 15 символов"], JSON_UNESCAPED_UNICODE);
    exit;
}

if (mb_strlen($password) < 4) {
    echo json_encode(["success" => false, "message" => "Пароль должен быть не короче 4 символов"], JSON_UNESCAPED_UNICODE);
    exit;
}

if(mb_strlen($address)>255) { echo json_encode(['success'=>false,'message'=>'Адрес должен содержать не более 255 символов'],JSON_UNESCAPED_UNICODE);exit; }
$passwordHash = password_hash($password, PASSWORD_DEFAULT);

$sql = "SELECT id FROM vladelci WHERE phone = ? LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$result = $stmt->get_result();
$client = mysqli_fetch_assoc($result);

if ($client) {
    echo json_encode(["success" => false, "message" => "Клиент с таким телефоном уже зарегистрирован"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "INSERT INTO vladelci (name, phone, password, address)
                               VALUES (?, ?, ?, NULLIF(?, ''))";
$stmt = $conn->prepare($sql);
$stmt->bind_param("ssss", $name, $phone, $passwordHash, $address);
$stmt->execute();
$id = mysqli_insert_id($conn);

echo json_encode([
    "success" => true,
    "message" => "Регистрация выполнена",
    "client" => ["id" => $id, "name" => $name, "phone" => $phone]
], JSON_UNESCAPED_UNICODE);


