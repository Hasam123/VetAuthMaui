<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require __DIR__ . "/../db.php";

$data = json_decode(file_get_contents("php://input"), true);
$id = (int)($data["id"] ?? 0);
$status = trim($data["status"] ?? "");

$allowed = ["new", "accepted", "done", "cancelled"];

if ($id <= 0 || !in_array($status, $allowed)) {
    echo json_encode(["success" => false, "message" => "Неверный статус"], JSON_UNESCAPED_UNICODE);
    exit;
}

$conn->begin_transaction();
try {
    $stmt = $conn->prepare("SELECT status FROM zapic_na_priem WHERE id = ? FOR UPDATE");
    $stmt->bind_param("i", $id);
    $stmt->execute();
    $appointment = $stmt->get_result()->fetch_assoc();
    if (!$appointment) {
        $conn->rollback();
        echo json_encode(["success" => false, "message" => "Запись не найдена"], JSON_UNESCAPED_UNICODE);
        exit;
    }
    if ($appointment["status"] === "done" && $status !== "done") {
        $conn->rollback();
        echo json_encode(["success" => false, "message" => "Завершённый приём нельзя вернуть в другой статус"], JSON_UNESCAPED_UNICODE);
        exit;
    }
    if ($status === "done") {
        $stmt = $conn->prepare("SELECT id FROM result_obsled WHERE zapic_id = ? AND (COALESCE(jaloba, '') <> '' OR COALESCE(diagnoz, '') <> '' OR COALESCE(result, '') <> '') UNION ALL SELECT id FROM lechenie WHERE zapic_id = ? AND (COALESCE(naz_lech, '') <> '' OR COALESCE(procedure_done, '') <> '' OR COALESCE(notes, '') <> '')");
        $stmt->bind_param("ii", $id, $id);
        $stmt->execute();
        if ($appointment["status"] === "cancelled" || $stmt->get_result()->num_rows === 0) {
            $conn->rollback();
            echo json_encode(["success" => false, "message" => "Сначала заполните медицинскую запись действующего приёма"], JSON_UNESCAPED_UNICODE);
            exit;
        }
    }
    $stmt = $conn->prepare("UPDATE zapic_na_priem SET status = ? WHERE id = ?");
    $stmt->bind_param("si", $status, $id);
    $stmt->execute();
    $conn->commit();
} catch (mysqli_sql_exception $e) {
    $conn->rollback();
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


