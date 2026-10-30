<?php
require_once __DIR__ . '/clinic_time.php';
function journalRequireMedical(string $jaloba, string $diagnoz, string $predDiag, string $nazLech, string $procedureDone): void {
    if(trim($jaloba)==='') { throw new InvalidArgumentException('Заполните жалобы / причину обращения'); }
    if(trim($diagnoz)==='' && trim($predDiag)==='') { throw new InvalidArgumentException('Заполните предварительный или заключительный диагноз'); }
    if(trim($nazLech)==='' && trim($procedureDone)==='') { throw new InvalidArgumentException('Укажите назначенное лечение или выполненные процедуры'); }
}
function journalDate(?string $value): ?string {
    if ($value===null || $value==='') { return null; }
    $date=DateTime::createFromFormat('!Y-m-d',$value);
    if(!$date || $date->format('Y-m-d')!==$value || $value>date('Y-m-d')) {
        throw new InvalidArgumentException('Укажите корректную дату начала заболевания, не позднее сегодняшнего дня');
    }
    return $value;
}
function journalPrimaryVisit(mysqli $db, int $current, ?int $primary): void {
    if($primary===null) { return; }
    $stmt=$db->prepare("SELECT p.id FROM zapic_na_priem c JOIN zapic_na_priem p ON p.pet_id=c.pet_id
        WHERE c.id=? AND p.id=? AND p.id<>c.id AND p.status='done' AND p.perv_priem_id IS NULL
        AND TIMESTAMP(p.data,p.time)<TIMESTAMP(c.data,c.time) FOR UPDATE");
    $stmt->bind_param('ii',$current,$primary);$stmt->execute();
    if(!$stmt->get_result()->num_rows) { throw new InvalidArgumentException('Выберите предыдущий первичный завершённый приём этого питомца'); }
}
