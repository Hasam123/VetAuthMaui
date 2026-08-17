<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET");

require "../../db.php";

$phone = trim($_GET["phone"] ?? "");

if ($phone == "") {
    echo json_encode(["success" => false, "message" => "Введите телефон"], JSON_UNESCAPED_UNICODE);
    exit;
}

$sql = "SELECT id FROM vladelci WHERE phone = ? LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$result = $stmt->get_result();
$client = mysqli_fetch_assoc($result);

if (!$client) {
    echo json_encode(["success" => true, "pets" => []], JSON_UNESCAPED_UNICODE);
    exit;
}

$clientId = (int)$client["id"];
$sql = "SELECT id, name, type, age, weight, last_vac_data
        FROM pets
        WHERE client_id = ?
        ORDER BY id DESC";
$stmt = $conn->prepare($sql);
$stmt->bind_param("i", $clientId);
$stmt->execute();
$result = $stmt->get_result();

$pets = [];
while ($row = mysqli_fetch_assoc($result)) {
    $pets[] = $row;
}

echo json_encode(["success" => true, "pets" => $pets], JSON_UNESCAPED_UNICODE);
