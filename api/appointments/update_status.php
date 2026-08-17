<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);
$id = (int)($data["id"] ?? 0);
$status = trim($data["status"] ?? "");

$allowed = ["new", "accepted", "done", "cancelled"];

if ($id <= 0 || !in_array($status, $allowed)) {
    echo json_encode(["success" => false, "message" => "Неверный статус"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "UPDATE zapic_na_priem SET status = ? WHERE id = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("si", $status, $id);

try {
    $stmt->execute();
} catch (mysqli_sql_exception $e) {
    if ((int)$e->getCode() === 1062) {
        echo json_encode([
            "success" => false,
            "message" => "Это время уже занято другой активной записью"
        ], JSON_UNESCAPED_UNICODE);
        exit;
    }

    error_log("Appointment status update failed: " . $e->getMessage());
    echo json_encode(["success" => false, "message" => "Не удалось обновить статус"], JSON_UNESCAPED_UNICODE);
    exit;
}

echo json_encode(["success" => true, "message" => "Статус обновлен"], JSON_UNESCAPED_UNICODE);


