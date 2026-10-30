<?php
header('Content-Type: application/json; charset=utf-8');
require __DIR__ . '/../db.php';
$data=json_decode(file_get_contents('php://input'),true);
$phone=trim($data['phone']??'');$address=trim($data['address']??'');
if($phone==='' || mb_strlen($address)>255) {
    echo json_encode(['success'=>false,'message'=>'Укажите телефон и адрес длиной до 255 символов'],JSON_UNESCAPED_UNICODE);exit;
}
$stmt=$conn->prepare('SELECT id FROM vladelci WHERE phone=?');$stmt->bind_param('s',$phone);$stmt->execute();
if(!$stmt->get_result()->num_rows) { echo json_encode(['success'=>false,'message'=>'Клиент не найден'],JSON_UNESCAPED_UNICODE);exit; }
$stmt=$conn->prepare("UPDATE vladelci SET address=NULLIF(?,'') WHERE phone=?");
$stmt->bind_param('ss',$address,$phone);$stmt->execute();
echo json_encode(['success'=>true,'message'=>'Адрес сохранён'],JSON_UNESCAPED_UNICODE);
