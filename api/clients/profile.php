<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET");

require __DIR__ . "/../db.php";

$phone = trim($_GET["phone"] ?? "");

if ($phone == "") {
    echo json_encode(["success" => false, "message" => "Введите номер телефона"], JSON_UNESCAPED_UNICODE);
    exit;
}

$requests = [];
$clientName = "";

$sql = "SELECT name, address FROM vladelci WHERE phone = ? LIMIT 1";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$client = mysqli_fetch_assoc($stmt->get_result());

if ($client) {
    $clientName = $client["name"];
}

$sql = "SELECT z.id, z.pet_id, z.perv_priem_id, v.address, p.pol AS pet_pol, z.doctor_id, doctor.name AS doctor_name, v.name, v.phone, p.name AS pet_name, p.type AS pet_type,
                                      p.age AS pet_age, s.name AS service_title,
                                      TIMESTAMP(z.data, z.time) AS appointment_at,
                                      z.client_comment, z.admin_comment, z.data_created AS data_created, z.status,
                                      m.jaloba, m.diagnoz, m.data_zabol, m.pred_diag, m.klin_prizn, m.ishod,
                                      m.naz_lech, m.procedure_done, m.notes AS treatment_notes
                               FROM zapic_na_priem z
                               JOIN pets p ON p.id = z.pet_id
                               LEFT JOIN admins doctor ON doctor.id = z.doctor_id
                               JOIN vladelci v ON v.id = p.client_id
                               LEFT JOIN services s ON s.id = z.service_id
                               LEFT JOIN medical_records m ON m.id = z.id
                               WHERE v.phone = ?
                               ORDER BY z.data_created DESC";
$stmt = $conn->prepare($sql);
$stmt->bind_param("s", $phone);
$stmt->execute();
$result = $stmt->get_result();

while ($row = mysqli_fetch_assoc($result)) {
    $requests[] = $row;
}

echo json_encode([
    "success" => true,
    "client" => ["name" => $clientName, "phone" => $phone, "address" => $client["address"] ?? ""],
    "requests" => $requests
], JSON_UNESCAPED_UNICODE);


