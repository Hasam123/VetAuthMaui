<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET");

require "../../db.php";

$phone = trim($_GET["phone"] ?? "");

if ($phone == "") {
    echo json_encode(["success" => false, "message" => "Введите номер телефона"], JSON_UNESCAPED_UNICODE);
    exit;
}

$requests = [];
$clientName = "";

$sql = "SELECT name FROM vladelci WHERE phone = ? LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$client = mysqli_fetch_assoc($stmt->get_result());

if ($client) {
    $clientName = $client["name"];
}

$sql = "SELECT z.id, v.name, v.phone, p.name AS pet_name, p.type AS pet_type,
                                      p.age AS pet_age, s.name AS service_title,
                                      TIMESTAMP(z.data, z.time) AS appointment_at,
                                      z.comment, z.admin_comment, z.created_at AS created, z.status,
                                      r.jaloba, r.diagnoz, r.result AS obsled_result,
                                      l.naz_lech, l.procedure_done, l.notes AS treatment_notes
                               FROM zapic_na_priem z
                               JOIN pets p ON p.id = z.pet_id
                               JOIN vladelci v ON v.id = p.client_id
                               LEFT JOIN services s ON s.id = z.service_id
                               LEFT JOIN result_obsled r ON r.zapic_id = z.id
                               LEFT JOIN lechenie l ON l.zapic_id = z.id
                               WHERE v.phone = ?
                               ORDER BY z.created_at DESC";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$result = $stmt->get_result();

while ($row = mysqli_fetch_assoc($result)) {
    $requests[] = $row;
}

echo json_encode([
    "success" => true,
    "client" => ["name" => $clientName, "phone" => $phone],
    "requests" => $requests
], JSON_UNESCAPED_UNICODE);


