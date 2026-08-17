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
    echo json_encode(["success" => false, "message" => "Неверные данные"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "SELECT z.status
                               FROM zapic_na_priem z
                               JOIN pets p ON p.id = z.pet_id
                               JOIN vladelci v ON v.id = p.client_id
                               WHERE z.id = ? AND v.phone = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("is", $id, $phone);
$stmt->execute();
$result = $stmt->get_result();
$row = mysqli_fetch_assoc($result);

if (!$row) {
    echo json_encode(["success" => false, "message" => "Заявка не найдена"], JSON_UNESCAPED_UNICODE);
    exit;
}

if ($row["status"] != "new" && $row["status"] != "accepted") {
    echo json_encode(["success" => false, "message" => "Эту заявку нельзя отменить"], JSON_UNESCAPED_UNICODE);
    exit;
}

$status = "cancelled";
$sql = "UPDATE zapic_na_priem SET status = ? WHERE id = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("si", $status, $id);
$stmt->execute();

echo json_encode(["success" => true, "message" => "Запись отменена"], JSON_UNESCAPED_UNICODE);


