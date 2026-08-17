<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require "../../db.php";

$data = json_decode(file_get_contents("php://input"), true);

$zapicId = (int)($data["id"] ?? 0);
$jaloba = trim($data["jaloba"] ?? "");
$diagnoz = trim($data["diagnoz"] ?? "");
$obsledResult = trim($data["obsled_result"] ?? "");
$nazLech = trim($data["naz_lech"] ?? "");
$procedureDone = trim($data["procedure_done"] ?? "");
$notes = trim($data["treatment_notes"] ?? "");

if ($zapicId <= 0) {
    echo json_encode(["success" => false, "message" => "Неверный ID записи"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "SELECT pet_id FROM zapic_na_priem WHERE id = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("i", $zapicId);
$stmt->execute();
$result = $stmt->get_result();
$zapic = mysqli_fetch_assoc($result);

if (!$zapic) {
    echo json_encode(["success" => false, "message" => "Запись не найдена"], JSON_UNESCAPED_UNICODE);
    exit;
}

$petId = (int)$zapic["pet_id"];
$today = date("Y-m-d");

$sql = "INSERT INTO result_obsled (zapic_id, jaloba, diagnoz, result)
                               VALUES (?, ?, ?, ?)
                               ON DUPLICATE KEY UPDATE jaloba = ?, diagnoz = ?, result = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("issssss", $zapicId, $jaloba, $diagnoz, $obsledResult, $jaloba, $diagnoz, $obsledResult);
$stmt->execute();

$sql = "INSERT INTO lechenie (pet_id, zapic_id, data, naz_lech, procedure_done, notes)
                               VALUES (?, ?, ?, ?, ?, ?)
                               ON DUPLICATE KEY UPDATE data = ?, naz_lech = ?, procedure_done = ?, notes = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("iissssssss", $petId, $zapicId, $today, $nazLech, $procedureDone, $notes, $today, $nazLech, $procedureDone, $notes);
$stmt->execute();

$doneStatus = "done";
$sql = "UPDATE zapic_na_priem SET status = ? WHERE id = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("si", $doneStatus, $zapicId);
$stmt->execute();

echo json_encode(["success" => true, "message" => "Медицинская запись сохранена"], JSON_UNESCAPED_UNICODE);


