<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST");
header("Access-Control-Allow-Headers: Content-Type");

require __DIR__ . "/../db.php";
require __DIR__ . "/../includes/journal.php";

$data = json_decode(file_get_contents("php://input"), true);

$zapicId = (int)($data["id"] ?? 0);
$jaloba = trim($data["jaloba"] ?? "");
$diagnoz = trim($data["diagnoz"] ?? "");
$nazLech = trim($data["naz_lech"] ?? "");
$procedureDone = trim($data["procedure_done"] ?? "");
$notes = trim($data["treatment_notes"] ?? "");

$predDiag=trim($data['pred_diag']??'');
$klinPrizn=trim($data['klin_prizn']??'');
$ishod=trim($data['ishod']??'');
if ($zapicId <= 0) {
    echo json_encode(["success" => false, "message" => "Неверный ID записи"], JSON_UNESCAPED_UNICODE);
    exit;
}

$conn->begin_transaction();
try {
    $sql = "SELECT pet_id, status, perv_priem_id, data FROM zapic_na_priem WHERE id = ? FOR UPDATE";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("i", $zapicId);
    $stmt->execute();
    $result = $stmt->get_result();
    $zapic = mysqli_fetch_assoc($result);

    if (!$zapic) {
        $conn->rollback();
        echo json_encode(["success" => false, "message" => "Запись не найдена"], JSON_UNESCAPED_UNICODE);
        exit;
    }

    if ($zapic["status"] === "cancelled") {
        $conn->rollback();
        echo json_encode(["success" => false, "message" => "Нельзя сохранить медкарту отменённой записи"], JSON_UNESCAPED_UNICODE);
        exit;
    }

    $stmt=$conn->prepare('SELECT data_zabol,pred_diag,klin_prizn,ishod FROM medical_records WHERE id=?');
    $stmt->bind_param('i',$zapicId);$stmt->execute();$old=$stmt->get_result()->fetch_assoc()?:[];
    $dataZabol=journalDate(array_key_exists('data_zabol',$data)?trim($data['data_zabol']??''):($old['data_zabol']??null));
    if($dataZabol!==null && $zapic['data']!==null && $dataZabol>$zapic['data']) {
        throw new InvalidArgumentException('Дата заболевания не может быть позднее даты приёма');
    }
    if(!array_key_exists('pred_diag',$data)) { $predDiag=$old['pred_diag']??''; }
    if(!array_key_exists('klin_prizn',$data)) { $klinPrizn=$old['klin_prizn']??''; }
    if(!array_key_exists('ishod',$data)) { $ishod=$old['ishod']??''; }
    journalRequireMedical($jaloba,$diagnoz,$predDiag,$nazLech,$procedureDone);
    if(mb_strlen($ishod)>100) { throw new InvalidArgumentException('Исход заболевания: не более 100 символов'); }
    $primary=$zapic['perv_priem_id'];
    if(array_key_exists('perv_priem_id',$data)) {
        $raw=$data['perv_priem_id'];
        if($raw===null || $raw==='' || $raw===0) { $primary=null; }
        else {
            $primary=filter_var($raw,FILTER_VALIDATE_INT,['options'=>['min_range'=>1]]);
            if($primary===false) { throw new InvalidArgumentException('Неверный ID первичного приёма'); }
        }
    }
    journalPrimaryVisit($conn,$zapicId,$primary===null?null:(int)$primary);
    // A root already used by repeat visits cannot itself become a repeat visit.
    if($primary!==null) {
        $stmt=$conn->prepare('SELECT id FROM zapic_na_priem WHERE perv_priem_id=? LIMIT 1');
        $stmt->bind_param('i',$zapicId);$stmt->execute();
        if($stmt->get_result()->num_rows) { throw new InvalidArgumentException('Этот приём уже является первичным для повторных обращений'); }
    }
    $sql="INSERT INTO medical_records(id,jaloba,diagnoz,naz_lech,procedure_done,notes,data_zabol,pred_diag,klin_prizn,ishod)
        VALUES(?,?,?,?,?,?,?,?,?,?) ON DUPLICATE KEY UPDATE jaloba=VALUES(jaloba),diagnoz=VALUES(diagnoz),
        naz_lech=VALUES(naz_lech),procedure_done=VALUES(procedure_done),notes=VALUES(notes),
        data_zabol=VALUES(data_zabol),pred_diag=VALUES(pred_diag),klin_prizn=VALUES(klin_prizn),ishod=VALUES(ishod)";
    $stmt=$conn->prepare($sql);
    $stmt->bind_param('isssssssss',$zapicId,$jaloba,$diagnoz,$nazLech,$procedureDone,$notes,$dataZabol,$predDiag,$klinPrizn,$ishod);
    $stmt->execute();
    $stmt=$conn->prepare("UPDATE zapic_na_priem SET status='done',perv_priem_id=? WHERE id=?");
    $stmt->bind_param('ii',$primary,$zapicId);$stmt->execute();
    $conn->commit();
} catch (Throwable $e) {
    $conn->rollback();
    if($e instanceof InvalidArgumentException) {
        echo json_encode(['success'=>false,'message'=>$e->getMessage()],JSON_UNESCAPED_UNICODE);exit;
    }
    error_log("Medical record save failed: " . $e->getMessage());
    echo json_encode(["success" => false, "message" => "Не удалось сохранить медицинскую запись"], JSON_UNESCAPED_UNICODE);
    exit;
}

echo json_encode(["success" => true, "message" => "Медицинская запись сохранена"], JSON_UNESCAPED_UNICODE);


