<?php
header('Content-Type: application/json; charset=utf-8');
require __DIR__.'/../../db.php';
require __DIR__.'/../../includes/visit_services.php';
if(!visitServicesReady($conn)){echo json_encode(['success'=>false,'message'=>'Сервер обновляется. Повторите позже'],JSON_UNESCAPED_UNICODE);exit;}
$id=(int)($_GET['id']??0); $doctor=(int)($_GET['doctor_id']??0);
$stmt=$conn->prepare('SELECT id,service_id,status FROM zapic_na_priem WHERE id=? AND doctor_id=?');
$stmt->bind_param('ii',$id,$doctor);$stmt->execute();$visit=$stmt->get_result()->fetch_assoc();
if(!$visit || $doctor<=0){echo json_encode(['success'=>false,'message'=>'Приём не найден'],JSON_UNESCAPED_UNICODE);exit;}
$rows=visitServicesAttach($conn,[$visit]);
echo json_encode(['success'=>true,'status'=>$visit['status'],'primary_service_id'=>(int)$visit['service_id'],
    'performed_services'=>$rows[0]['performed_services'],'services_total_price'=>$rows[0]['services_total_price']],JSON_UNESCAPED_UNICODE);
