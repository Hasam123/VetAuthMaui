<?php
// Loaded inside the disposable database / HTTP server harness in clinic_journal.php.
if (!isset($source,$base,$db) || !preg_match('/^vet_journaltest_[a-f0-9]{12}$/D',$name)) {
    throw new RuntimeException('Workflow checks require the isolated integration harness');
}
function workflowApi($path,$payload=null) {
    try { return api($path,$payload); }
    catch(Throwable $e) { check(false,'Workflow API returns JSON: '.$path); return ['success'=>false]; }
}
$wfPhone='W'.bin2hex(random_bytes(6));
$wfRegistration=['name'=>'Иванов Иван Иванович','phone'=>$wfPhone,'password'=>'workflow-pass','address'=>'Тирасполь, тестовый адрес'];
foreach(['name','phone','password','address'] as $field) {
    check(!api('clients/register.php',array_replace($wfRegistration,[$field=>'']))['success'],'Workflow registration requires '.$field);
}
check(!api('clients/register.php',array_replace($wfRegistration,['password'=>'123']))['success'],'Workflow rejects short password');
check(!api('clients/register.php',array_replace($wfRegistration,['phone'=>str_repeat('1',16)]))['success'],'Workflow rejects long phone');
check(api('clients/register.php',$wfRegistration)['success'],'Workflow register client');
check(!api('clients/register.php',$wfRegistration)['success'],'Workflow rejects duplicate phone');
$wfOwner=(int)scalar("SELECT id FROM vladelci WHERE phone='$wfPhone'");
$wfHash=scalar("SELECT password FROM vladelci WHERE id=$wfOwner");
check($wfHash!=='workflow-pass' && password_verify('workflow-pass',$wfHash),'Workflow password stored as hash');
check(!api('clients/login.php',['phone'=>$wfPhone,'password'=>'wrong'])['success'],'Workflow rejects wrong client password');
check(!api('clients/login.php',['phone'=>"' OR 1=1 --",'password'=>'wrong'])['success'],'Workflow login injection rejected');
$wfLogin=api('clients/login.php',['phone'=>$wfPhone,'password'=>'workflow-pass']);
check($wfLogin['success'] && (int)$wfLogin['client']['id']===$wfOwner,'Workflow client login returns identity');
check(!isset($wfLogin['client']['password']),'Workflow login does not expose password');
$wfAdminLogin='workflow_'.bin2hex(random_bytes(5));$wfAdminHash=password_hash('doctor-pass',PASSWORD_DEFAULT);
$stmt=$db->prepare('INSERT INTO admins(login,password,name,is_active) VALUES(?,?,?,1)');$wfDoctorName='Тестовый ветеринар';
$stmt->bind_param('sss',$wfAdminLogin,$wfAdminHash,$wfDoctorName);$stmt->execute();$wfDoctor=(int)$db->insert_id;
check(!api('admin/login.php',['login'=>$wfAdminLogin,'password'=>'wrong'])['success'],'Workflow rejects wrong veterinarian password');
$wfLogin=api('admin/login.php',['login'=>$wfAdminLogin,'password'=>'doctor-pass']);
check($wfLogin['success'] && (int)$wfLogin['user']['id']===$wfDoctor,'Workflow veterinarian login');
check(!isset($wfLogin['user']['password']),'Workflow veterinarian hash is not returned');
$wfDoctors=api('doctors/list.php')['doctors'];
check(in_array($wfDoctor,array_column($wfDoctors,'id')),'Workflow active veterinarian listed');
$db->query("UPDATE admins SET is_active=0 WHERE id=$wfDoctor");
check(!in_array($wfDoctor,array_column(api('doctors/list.php')['doctors'],'id')),'Workflow inactive veterinarian hidden');
$wfInactiveSchedule=api('schedule/free_slots.php?doctor_id='.$wfDoctor);$wfInactiveAvailable=[];
foreach($wfInactiveSchedule['days'] as $day)foreach($day['slots'] as $slot)if($slot['is_available'])$wfInactiveAvailable[]=$slot;
check(count($wfInactiveAvailable)===0,'Workflow inactive veterinarian has no available slots');
$db->query("UPDATE admins SET is_active=1 WHERE id=$wfDoctor");
$wfServiceData=['title'=>'Тестовый осмотр','description'=>'Проверка полного цикла','category'=>'Терапия','price'=>350];
check(!api('services/create.php',array_replace($wfServiceData,['price'=>0]))['success'],'Workflow rejects zero service price');
check(!api('services/create.php',array_replace($wfServiceData,['category'=>'']))['success'],'Workflow service category required');
$wfCreated=api('services/create.php',$wfServiceData);check($wfCreated['success'],'Workflow create service');$wfService=(int)$wfCreated['id'];
check(api('services/update.php',array_replace($wfServiceData,['id'=>$wfService,'price'=>400]))['success'],'Workflow edit service');
$wfListed=array_values(array_filter(api('services/list.php')['services'],fn($r)=>(int)$r['id']===$wfService));
check(count($wfListed)===1 && $wfListed[0]['price']===400 && $wfListed[0]['category']==='Терапия','Workflow service list reflects edit');
$wfPetData=['phone'=>$wfPhone,'name'=>'Рекс','type'=>'Собака','age'=>'4 года','weight'=>'12,50','pol'=>'Самец','last_vac_data'=>'2026-01-10'];
check(!api('pets/create.php',array_replace($wfPetData,['weight'=>'-1']))['success'],'Workflow rejects negative pet weight');
check(!api('pets/create.php',array_replace($wfPetData,['weight'=>'1000']))['success'],'Workflow rejects excessive pet weight');
check(api('pets/create.php',$wfPetData)['success'],'Workflow add pet');
$wfPets=api('pets/list.php?phone='.$wfPhone)['pets'];$wfPet=(int)$wfPets[0]['id'];
check(count($wfPets)===1 && $wfPets[0]['weight']==='12.50' && $wfPets[0]['last_vac_data']==='2026-01-10','Workflow pet decimal comma and vaccination round trip');
check(!api('pets/update.php',array_replace($wfPetData,['id'=>$wfPet,'phone'=>'missing']))['success'],'Workflow rejects editing pet of another owner');
check(!api('pets/delete.php',['id'=>$wfPet,'phone'=>'missing'])['success'],'Workflow rejects deleting pet of another owner');
check(api('pets/update.php',array_replace($wfPetData,['id'=>$wfPet,'name'=>'Рекс обновлённый']))['success'],'Workflow edit pet');
$wfSlots=api('schedule/free_slots.php?doctor_id='.$wfDoctor);$wfAvailable=[];
foreach($wfSlots['days'] as $day)foreach($day['slots'] as $slot)if($slot['is_available'])$wfAvailable[]=$slot['time'];
check(count($wfAvailable)>2,'Workflow veterinarian has available slots');
$wfBooking=['name'=>$wfRegistration['name'],'phone'=>$wfPhone,'pet_id'=>$wfPet,'pet_name'=>'Рекс обновлённый','pet_type'=>'Собака',
    'doctor_id'=>$wfDoctor,'service_id'=>$wfService,'appointment_at'=>$wfAvailable[0],'client_comment'=>'Первичный осмотр'];
foreach(['doctor_id'=>0,'service_id'=>2147483647,'pet_id'=>2147483647] as $field=>$bad)
    check(!api('appointments/create.php',array_replace($wfBooking,[$field=>$bad]))['success'],'Workflow booking rejects invalid '.$field);
check(api('appointments/create.php',$wfBooking)['success'],'Workflow book appointment');
$wfAppointment=(int)scalar("SELECT MAX(id) FROM zapic_na_priem WHERE pet_id=$wfPet");
check(scalar("SELECT status FROM zapic_na_priem WHERE id=$wfAppointment")==='new','Workflow booking starts new');
check(!api('appointments/cancel.php',['id'=>$wfAppointment,'phone'=>'missing'])['success'],'Workflow another owner cannot cancel');
check(!api('pets/delete.php',['id'=>$wfPet,'phone'=>$wfPhone])['success'],'Workflow pet with appointments protected');
check(api('appointments/update_status.php',['id'=>$wfAppointment,'status'=>'accepted'])['success'],'Workflow veterinarian accepts appointment');
check(api('appointments/update_admin_comment.php',['id'=>$wfAppointment,'admin_comment'=>"Комментарий ' 🐕"])['success'],'Workflow veterinarian saves comment');
check(!api('appointments/update_status.php',['id'=>$wfAppointment,'status'=>'unknown'])['success'],'Workflow rejects invalid status');
check(!api('appointments/update_status.php',['id'=>$wfAppointment,'status'=>'done'])['success'],'Workflow cannot complete without medical card');
$wfCount=(int)scalar('SELECT COUNT(*) FROM service_history');
$wfCard=['jaloba'=>'Профилактический осмотр','pred_diag'=>'Клинически здоров','procedure_done'=>'Осмотр','treatment_notes'=>'Наблюдение'];
check(saveCard($wfAppointment,$wfCard)['success'],'Workflow save medical card and complete');
check(scalar("SELECT status FROM zapic_na_priem WHERE id=$wfAppointment")==='done','Workflow medical card completes appointment');
check((int)scalar('SELECT COUNT(*) FROM service_history')===$wfCount+1,'Workflow completion adds one history row');
$wfSnapshot=$db->query("SELECT * FROM service_history WHERE pet_id=$wfPet ORDER BY id DESC LIMIT 1")->fetch_assoc();
check($wfSnapshot['client_name']===$wfRegistration['name'] && $wfSnapshot['phone']===$wfPhone
    && $wfSnapshot['procedure_done']==='Осмотр' && (int)$wfSnapshot['total_price']===400,'Workflow snapshot contains copied owner, card and price');
$wfProfile=api('clients/profile.php?phone='.$wfPhone);
$wfRequest=array_values(array_filter($wfProfile['requests'],fn($r)=>(int)$r['id']===$wfAppointment))[0];
check($wfRequest['status']==='done' && $wfRequest['procedure_done']==='Осмотр','Workflow completed procedures visible to client');
check(!api('appointments/cancel.php',['id'=>$wfAppointment,'phone'=>$wfPhone])['success'],'Workflow client cannot cancel completed visit');
check(!api('appointments/update_status.php',['id'=>$wfAppointment,'status'=>'accepted'])['success'],'Workflow cannot reopen completed visit');
check(!api('appointments/delete.php',['id'=>$wfAppointment])['success'],'Workflow completed medical record protected from deletion');
$wfFiltered=api('appointments/list.php?doctor_id='.$wfDoctor)['requests'];
check(count($wfFiltered)===1 && (int)$wfFiltered[0]['id']===$wfAppointment,'Workflow veterinarian sees filtered appointments');
check(api('appointments/create.php',array_replace($wfBooking,['appointment_at'=>$wfAvailable[1]]))['success'],'Workflow create appointment for cancellation');
$wfCancelId=(int)scalar("SELECT MAX(id) FROM zapic_na_priem WHERE pet_id=$wfPet");
check(api('appointments/cancel.php',['id'=>$wfCancelId,'phone'=>$wfPhone])['success'],'Workflow client cancels active appointment');
check(!api('appointments/cancel.php',['id'=>$wfCancelId,'phone'=>$wfPhone])['success'],'Workflow repeated cancellation rejected');
check(api('appointments/delete.php',['id'=>$wfCancelId])['success'],'Workflow delete cancelled appointment without card');
$wfStats=api('admin/stats.php')['stats'];
foreach(['new','accepted','done','cancelled'] as $status)
    check((int)$wfStats['requests_'.$status]===(int)scalar("SELECT COUNT(*) FROM zapic_na_priem WHERE status='$status'"),'Workflow statistics match database: '.$status);
check((int)$wfStats['requests_total']===(int)scalar("SELECT COUNT(*) FROM zapic_na_priem WHERE status IN ('new','accepted','done','cancelled')"),'Workflow statistics total matches database');
check((int)$wfStats['services_total']===(int)scalar('SELECT COUNT(*) FROM services'),'Workflow service statistics match database');
check(api('pets/create.php',array_replace($wfPetData,['name'=>'Удаляемый питомец']))['success'],'Workflow add pet for deletion');
$wfDisposablePet=(int)scalar("SELECT MAX(id) FROM pets WHERE client_id=$wfOwner");
check(api('pets/delete.php',['id'=>$wfDisposablePet,'phone'=>$wfPhone])['success'],'Workflow delete pet without appointments');
$wfUnusedService=api('services/create.php',array_replace($wfServiceData,['title'=>'Удаляемая услуга']));
check($wfUnusedService['success'] && api('services/delete.php',['id'=>$wfUnusedService['id']])['success'],'Workflow delete unused service');
check(!api('appointments/update_admin_comment.php',['id'=>2147483647,'admin_comment'=>'test'])['success'],'Workflow missing appointment comment rejected');
check(!api('appointments/delete.php',['id'=>2147483647])['success'],'Workflow missing appointment deletion rejected');
// Exploratory boundary tests: failures are reported; the disposable database is discarded.
check(!workflowApi('services/delete.php',['id'=>$wfService])['success'],'Workflow referenced service deletion rejected');
check(!workflowApi('services/update.php',array_replace($wfServiceData,['id'=>2147483647]))['success'],'Workflow missing service update rejected');
check(!workflowApi('pets/create.php',array_replace($wfPetData,['last_vac_data'=>'2026-02-30']))['success'],'Workflow invalid vaccination date rejected');
check(!workflowApi('pets/update.php',array_replace($wfPetData,['id'=>$wfPet,'last_vac_data'=>'2026-02-30']))['success'],'Workflow invalid vaccination date update rejected');
// No cookies or Authorization header are sent by api(). A login response alone is not server authentication.
check(!api('admin/stats.php')['success'],'Security anonymous veterinarian statistics denied');
check(!api('clients/profile.php?phone='.$wfPhone)['success'],'Security anonymous client medical profile denied');
check(!api('appointments/update_admin_comment.php',['id'=>$wfAppointment,'admin_comment'=>'Anonymous modification'])['success'],
    'Security anonymous appointment modification denied');
