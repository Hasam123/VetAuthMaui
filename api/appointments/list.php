<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET");

require "../../db.php";

$requests = [];

$sql = "SELECT z.id, v.name, v.phone,
               p.name AS pet_name, p.type AS pet_type, p.age AS pet_age,
               z.service_id, s.name AS service_title,
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
        WHERE z.status IN ('new', 'accepted', 'done', 'cancelled')
        ORDER BY z.created_at DESC";

$stmt = $conn->prepare($sql);
$stmt->execute();
$result = $stmt->get_result();

while ($row = mysqli_fetch_assoc($result)) {
    $requests[] = $row;
}

echo json_encode(["success" => true, "requests" => $requests], JSON_UNESCAPED_UNICODE);


