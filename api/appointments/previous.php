<?php
header('Content-Type: application/json; charset=utf-8');
require __DIR__ . '/../../db.php';
$id=(int)($_GET['id']??0);
$stmt=$conn->prepare("SELECT p.id, TIMESTAMP(p.data,p.time) AS primary_at,
        last_visit.id AS last_visit_id, TIMESTAMP(last_visit.data,last_visit.time) AS appointment_at, d.name AS doctor_name,
        s.name AS service_title, last_visit.perv_priem_id, m.jaloba, m.diagnoz, m.naz_lech,
        m.procedure_done, m.notes AS treatment_notes, m.data_zabol, m.pred_diag, m.klin_prizn, m.ishod
    FROM zapic_na_priem current_visit
    JOIN zapic_na_priem p ON p.pet_id=current_visit.pet_id
    JOIN zapic_na_priem last_visit ON last_visit.id=(
        SELECT lv.id FROM zapic_na_priem lv
        WHERE (lv.id=p.id OR lv.perv_priem_id=p.id) AND lv.pet_id=current_visit.pet_id
          AND lv.status='done' AND TIMESTAMP(lv.data,lv.time)<TIMESTAMP(current_visit.data,current_visit.time)
        ORDER BY lv.data DESC,lv.time DESC,lv.id DESC LIMIT 1)
    LEFT JOIN admins d ON d.id=last_visit.doctor_id
    LEFT JOIN services s ON s.id=last_visit.service_id
    LEFT JOIN medical_records m ON m.id=last_visit.id
    WHERE current_visit.id=? AND p.id<>current_visit.id AND p.status='done'
      AND p.perv_priem_id IS NULL
      AND TIMESTAMP(p.data,p.time)<TIMESTAMP(current_visit.data,current_visit.time)
    ORDER BY p.data DESC,p.time DESC,p.id DESC");
$stmt->bind_param('i',$id);$stmt->execute();
echo json_encode(['success'=>true,'visits'=>$stmt->get_result()->fetch_all(MYSQLI_ASSOC)],JSON_UNESCAPED_UNICODE);
