<?php
// Integration tests: production is read-only; all writes go to a disposable clone.
mysqli_report(MYSQLI_REPORT_ERROR|MYSQLI_REPORT_STRICT);
date_default_timezone_set('Europe/Chisinau');
$root=dirname(__DIR__);$name='vet_journaltest_'.bin2hex(random_bytes(6));
$temp=sys_get_temp_dir().DIRECTORY_SEPARATOR.$name;
$db=new mysqli('localhost','root','');$db->set_charset('utf8mb4');
$passed=0;$failed=0;$checks=[];$server=null;
function check($ok,$label) { global $passed,$failed,$checks; $ok=(bool)$ok;$ok?$passed++:$failed++;$checks[]=['check'=>$label,'passed'=>$ok];echo ($ok?'PASS ':'FAIL ').$label.PHP_EOL; }
function scalar($sql) { global $db;return $db->query($sql)->fetch_row()[0]; }
function api($path,$data=null) {
    global $base;
    $body=file_get_contents($base.'/api/'.$path,false,stream_context_create(['http'=>[
        'method'=>$data===null?'GET':'POST','header'=>"Content-Type: application/json\r\n",'timeout'=>10,
        'ignore_errors'=>true,'content'=>$data===null?'':json_encode($data,JSON_UNESCAPED_UNICODE)]]));
    $json=json_decode($body,true);
    if(!is_array($json)) {throw new RuntimeException('Invalid JSON from '.$path.': '.substr($body,0,200));}return $json;
}
function copyDir($from,$to) {mkdir($to,0777,true);foreach(new DirectoryIterator($from) as $f){if($f->isDot())continue;$dest=$to.'/'.$f->getFilename();$f->isDir()?copyDir($f->getPathname(),$dest):copy($f->getPathname(),$dest);}}
function deleteDir($path) {if(!is_dir($path))return;foreach(new DirectoryIterator($path) as $f){if($f->isDot())continue;$f->isDir()?deleteDir($f->getPathname()):unlink($f->getPathname());}rmdir($path);}
function visit($pet,$offset,$status='accepted') {
    global $db,$doctor,$service,$visitCounter;
    $visitCounter++;$date=date('Y-m-d',strtotime("$offset days"));$clock=sprintf('%02d:%02d:00',8+intdiv($visitCounter,2),($visitCounter%2)*30);
    $stmt=$db->prepare('INSERT INTO zapic_na_priem(pet_id,service_id,doctor_id,data,time,status,client_comment) VALUES(?,?,?,?,?,?,?)');$comment='Test';
    $stmt->bind_param('iiissss',$pet,$service,$doctor,$date,$clock,$status,$comment);$stmt->execute();return (int)$db->insert_id;
}
function saveCard($id,$fields) {return api('appointments/update_medical_record.php',['id'=>$id]+$fields);}
try {
    $hashes=[];$source='vet_clinic';
    $tables=$db->query("SHOW FULL TABLES FROM `$source` WHERE Table_type='BASE TABLE'")->fetch_all();
    foreach($tables as [$t]) {$hashes[$t]=hash('sha256',serialize($db->query("SELECT * FROM `$source`.`$t` ORDER BY id")->fetch_all(MYSQLI_ASSOC)));}
    $db->query("CREATE DATABASE `$name` CHARACTER SET utf8mb4");$db->select_db($name);$db->query('SET FOREIGN_KEY_CHECKS=0');
    foreach($tables as [$t]) {
        $db->query($db->query("SHOW CREATE TABLE `$source`.`$t`")->fetch_row()[1]);
        $cols=array_filter($db->query("SHOW COLUMNS FROM `$t`")->fetch_all(MYSQLI_ASSOC),fn($r)=>stripos($r['Extra'],'GENERATED')===false);
        $fields=implode(',',array_map(fn($r)=>'`'.$r['Field'].'`',$cols));
        $db->query("INSERT INTO `$t` ($fields) SELECT $fields FROM `$source`.`$t`");
    }
    $db->query('SET FOREIGN_KEY_CHECKS=1');
    foreach($db->query("SHOW TRIGGERS FROM `$source`")->fetch_all(MYSQLI_ASSOC) as $t){$db->query($db->query("SHOW CREATE TRIGGER `$source`.`{$t['Trigger']}`")->fetch_assoc()['SQL Original Statement']);}
    foreach(['vladelci'=>['address'],'pets'=>['pol'],'zapic_na_priem'=>['perv_priem_id'],'medical_records'=>['data_zabol','pred_diag','klin_prizn','ishod']] as $t=>$cols) {
        $actual=array_column($db->query("SHOW COLUMNS FROM `$t`")->fetch_all(MYSQLI_ASSOC),'Field');foreach($cols as $c)check(in_array($c,$actual),"Schema $t.$c");
    }
    copyDir($root.'/api',$temp.'/api');
    file_put_contents($temp.'/api/db.php','<?php mysqli_report(MYSQLI_REPORT_ERROR|MYSQLI_REPORT_STRICT); $conn=new mysqli("localhost","root","","'.$name.'");$conn->set_charset("utf8mb4");');
    $socket=stream_socket_server('tcp://127.0.0.1:0');$address=stream_socket_get_name($socket,false);fclose($socket);$base='http://'.$address;
    $server=proc_open([PHP_BINARY,'-S',$address,'-t',$temp],[0=>['pipe','r'],1=>['file',$temp.'/server.log','a'],2=>['file',$temp.'/server.log','a']],$pipes,$temp);
    if(!is_resource($server))throw new RuntimeException('Cannot start test server');
    for($i=0;$i<50;$i++){ $probe=@fsockopen('127.0.0.1',(int)substr(strrchr($address,':'),1));if($probe){fclose($probe);break;}usleep(100000); }
    $phone='J'.bin2hex(random_bytes(6));$addressText="Тирасполь, ул. Тестовая, 1 ' 🏠";
    $ownersBefore=(int)scalar('SELECT COUNT(*) FROM vladelci');
    foreach([null,'',"  \n "] as $badAddress) {
        $registration=['name'=>'Тест','phone'=>$phone,'password'=>'test-pass'];
        if($badAddress!==null)$registration['address']=$badAddress;
        check(!api('clients/register.php',$registration)['success'],'Registration rejects missing, empty or whitespace address');
    }
    check((int)scalar('SELECT COUNT(*) FROM vladelci')===$ownersBefore,'Invalid registration does not create client');
    check(api('clients/register.php',['name'=>'Тест','phone'=>$phone,'password'=>'test-pass','address'=>$addressText])['success'],'Register with address');
    check(api('clients/login.php',['phone'=>$phone,'password'=>'test-pass'])['success'],'Client login');
    check(api('clients/profile.php?phone='.$phone)['client']['address']===$addressText,'Address round trip');
    check(!api('clients/update_address.php',['phone'=>$phone,'address'=>str_repeat('я',256)])['success'],'Long address rejected');
    check(api('clients/update_address.php',['phone'=>$phone,'address'=>'Новый адрес'])['success'],'Update address');
    check(api('clients/profile.php?phone='.$phone)['client']['address']==='Новый адрес','Updated address read');
    check(api('clients/update_address.php',['phone'=>$phone,'address'=>''])['success'],'Clear optional address');
    check(!api('clients/update_address.php',['phone'=>'missing','address'=>'test'])['success'],'Missing client address update rejected');
    $owner=(int)scalar("SELECT id FROM vladelci WHERE phone='$phone'");
    check(api('pets/create.php',['phone'=>$phone,'name'=>'Барсик','type'=>'Кошка','pol'=>'Самец'])['success'],'Create male pet');
    $pet=(int)scalar("SELECT MAX(id) FROM pets WHERE client_id=$owner");
    check(api('pets/list.php?phone='.$phone)['pets'][0]['pol']==='Самец','Pet sex round trip');
    check(!api('pets/create.php',['phone'=>$phone,'name'=>'Invalid','type'=>'Кошка','pol'=>'Invalid'])['success'],'Invalid sex rejected');
    $petData=['id'=>$pet,'phone'=>$phone,'name'=>'Барсик','type'=>'Кошка'];
    check(api('pets/update.php',$petData+['pol'=>'Самка'])['success'],'Update pet sex');
    check(api('pets/update.php',$petData)['success'] && scalar("SELECT pol FROM pets WHERE id=$pet")==='Самка','Omitted sex preserves existing value');
    check(api('pets/update.php',$petData+['pol'=>''])['success'] && scalar("SELECT pol FROM pets WHERE id=$pet")===null,'Clear sex to NULL');
    check(!api('pets/update.php',$petData+['pol'=>'Invalid'])['success'],'Invalid sex update rejected');
    check(api('pets/create.php',['phone'=>$phone,'name'=>'Другой','type'=>'Кошка'])['success'],'Create pet without sex');
    $pet2=(int)scalar("SELECT MAX(id) FROM pets WHERE client_id=$owner");
    $doctor=(int)scalar('SELECT id FROM admins WHERE is_active=1 LIMIT 1');
    $db->query("INSERT INTO services(name,category,description,price) VALUES('Test','Test','Test',250)");$service=(int)$db->insert_id;$visitCounter=0;
    $db->query("UPDATE vladelci SET address='Адрес на момент приёма' WHERE id=$owner");
    $db->query("UPDATE pets SET pol='Самец',age='2 года',weight=4.25 WHERE id=$pet");
    $primary=visit($pet,-3);
    $fields=['jaloba'=>"Боль ' 🐱",'diagnoz'=>'Отит','naz_lech'=>'Назначение','procedure_done'=>'Осмотр','treatment_notes'=>'Заметки',
        'data_zabol'=>date('Y-m-d',strtotime('-4 days')),'pred_diag'=>'Подозрение на отит','klin_prizn'=>"Покраснение\nИсследование",'ishod'=>'Улучшение'];
    check(saveCard($primary,$fields)['success'],'Save primary card with all journal fields');
    $saved=$db->query("SELECT * FROM medical_records WHERE id=$primary")->fetch_assoc();
    foreach(['data_zabol','pred_diag','klin_prizn','ishod'] as $f)check($saved[$f]===$fields[$f],'Medical field round trip: '.$f);
    $history=$db->query('SELECT * FROM service_history ORDER BY id DESC LIMIT 1')->fetch_assoc();$historyCount=(int)scalar('SELECT COUNT(*) FROM service_history');
    $expectedSnapshot=['client_id'=>$owner,'client_name'=>'Тест','pet_id'=>$pet,'pet_name'=>'Барсик',
        'service_name'=>'Test','doctor_name'=>scalar("SELECT name FROM admins WHERE id=$doctor"),
        'address'=>'Адрес на момент приёма','pet_type'=>'Кошка','pol'=>'Самец','pet_age'=>'2 года',
        'service_category'=>'Test','vid_priema'=>'Первичный','kol'=>1,'total_price'=>250,
        'phone'=>$phone,'weight'=>'4.25','data_zabol'=>$fields['data_zabol'],'jaloba'=>$fields['jaloba'],
        'pred_diag'=>$fields['pred_diag'],'diagnoz'=>$fields['diagnoz'],'klin_prizn'=>$fields['klin_prizn'],
        'naz_lech'=>$fields['naz_lech'],'procedure_done'=>$fields['procedure_done'],
        'ishod'=>$fields['ishod'],'notes'=>$fields['treatment_notes']];
    foreach($expectedSnapshot as $field=>$value)check((string)$history[$field]===(string)$value,'Copied history field: '.$field);
    check(!empty($history['data_created']) && (int)$history['id']>0,'History generates own ID and creation timestamp');
    check($history['kol']==1 && $history['doctor_name']!==null,'History still includes doctor and quantity');
    check(saveCard($primary,$fields)['success'] && (int)scalar('SELECT COUNT(*) FROM service_history')===$historyCount,'Repeated save does not duplicate history');
    check(saveCard($primary,['jaloba'=>'Боль','diagnoz'=>'Уточнение','naz_lech'=>'Назначение'])['success'],'Old request format still accepted');
    check(scalar("SELECT klin_prizn FROM medical_records WHERE id=$primary")===$fields['klin_prizn'],'Old request preserves journal fields');
    $repeat=visit($pet,-2);
    $prev=api('appointments/previous.php?id='.$repeat)['visits'];
    check(in_array($primary,array_map(fn($r)=>(int)$r['id'],$prev)),'Primary selection lists previous visit');
    $previousCard=array_values(array_filter($prev,fn($r)=>(int)$r['id']===$primary))[0];
    $clientCards=api('clients/profile.php?phone='.$phone)['requests'];
    $clientCard=array_values(array_filter($clientCards,fn($r)=>(int)$r['id']===$primary))[0];
    foreach(['jaloba','diagnoz','naz_lech','procedure_done','treatment_notes','data_zabol','pred_diag','klin_prizn','ishod'] as $field) {
        check($previousCard[$field]===$clientCard[$field],'Previous card matches client view: '.$field);
    }
    check((int)$previousCard['last_visit_id']===$primary,'First repeat displays primary visit');
    check(saveCard($repeat,$fields+['perv_priem_id'=>$primary])['success'],'Save repeat linked to primary');
    check((int)scalar("SELECT perv_priem_id FROM zapic_na_priem WHERE id=$repeat")===$primary,'Primary relation stored');
    check(scalar('SELECT vid_priema FROM service_history ORDER BY id DESC LIMIT 1')==='Повторный','Repeat visit kind copied into history');
    foreach(['appointments/list.php?doctor_id='.$doctor,'clients/profile.php?phone='.$phone,'schedule/get_zapis_admin.php?doctor_id='.$doctor] as $path) {
        $r=api($path);$found=array_values(array_filter($r['requests'],fn($r)=>(int)$r['id']===$repeat));
        check(count($found)===1 && $found[0]['klin_prizn']===$fields['klin_prizn'] && (int)$found[0]['perv_priem_id']===$primary,'Read journal and primary link: '.$path);
    }
    $other=visit($pet2,-3);check(saveCard($other,$fields)['success'],'Prepare other pet primary');
    $later=visit($pet,-1);check(saveCard($later,$fields)['success'],'Prepare later primary');
    $pending=visit($pet,-3);$cancelled=visit($pet,-3,'cancelled');
    foreach([[$repeat,'self'],[$other,'different pet'],[$later,'later visit'],[$pending,'unfinished visit'],[$cancelled,'cancelled visit'],[2147483647,'missing visit'],[-1,'negative ID'],['abc','invalid ID']] as [$id,$label]) {
        check(!saveCard($repeat,$fields+['perv_priem_id'=>$id])['success'],'Reject primary: '.$label);
    }
    $next=visit($pet,-1);
    $nextChoices=api('appointments/previous.php?id='.$next)['visits'];
    $nextPrevious=array_values(array_filter($nextChoices,fn($r)=>(int)$r['id']===$primary))[0];
    check((int)$nextPrevious['last_visit_id']===$repeat,'Further repeat displays latest visit of same illness');
    check(!saveCard($next,$fields+['perv_priem_id'=>$repeat])['success'],'Reject chain of repeat visits');
    $earlier=visit($pet,-5);$earlierFields=array_replace($fields,['data_zabol'=>date('Y-m-d',strtotime('-6 days'))]);
    check(saveCard($earlier,$earlierFields)['success'],'Prepare earlier root');
    check(!saveCard($primary,$fields+['perv_priem_id'=>$earlier])['success'],'Root with repeat visits cannot become repeat');
    foreach(['2026-02-30',date('Y-m-d',strtotime('+1 day')),date('Y-m-d')] as $bad) {
        check(!saveCard($repeat,array_replace($fields,['data_zabol'=>$bad]))['success'],'Invalid or inconsistent disease date rejected: '.$bad);
    }
    check(!saveCard($repeat,array_replace($fields,['ishod'=>str_repeat('я',101)]))['success'],'Long outcome rejected');
    check(!saveCard($next,[])['success'],'Empty medical card rejected');
    check(!saveCard($cancelled,$fields)['success'],'Cancelled visit rejects save');
    $before=$db->query("SELECT * FROM medical_records WHERE id=$repeat")->fetch_assoc();
    $db->query("CREATE TRIGGER test_fail_status BEFORE UPDATE ON zapic_na_priem FOR EACH ROW BEGIN IF NEW.id=$repeat THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected error'; END IF; END");
    check(!saveCard($repeat,array_replace($fields,['klin_prizn'=>'Must roll back','perv_priem_id'=>null]))['success'],'Injected failure reported');
    check($before===$db->query("SELECT * FROM medical_records WHERE id=$repeat")->fetch_assoc(),'Medical fields rolled back');
    check((int)scalar("SELECT perv_priem_id FROM zapic_na_priem WHERE id=$repeat")===$primary,'Primary relation rolled back');
    $db->query('DROP TRIGGER test_fail_status');
    check(saveCard($repeat,array_replace($fields,['data_zabol'=>'','perv_priem_id'=>null]))['success'],'Clear optional disease date and primary relation');
    check(scalar("SELECT data_zabol FROM medical_records WHERE id=$repeat")===null && scalar("SELECT perv_priem_id FROM zapic_na_priem WHERE id=$repeat")===null,'Cleared fields stored as NULL');
    try{$db->query("UPDATE zapic_na_priem SET perv_priem_id=2147483647 WHERE id=$repeat");check(false,'Primary FK rejects missing visit');}catch(mysqli_sql_exception $e){check($e->getCode()===1452,'Primary FK rejects missing visit');}
    check(!api('appointments/delete.php',['id'=>$primary])['success'],'Medical history protected from deletion');
    $slots=api('schedule/free_slots.php?doctor_id='.$doctor);$available=[];
    check($slots['timezone']==='Europe/Chisinau' && $slots['server_now']>0,'Schedule supplies clinic clock');
    $slotTimesCorrect=true;$noPastAvailable=true;
    foreach($slots['days'] as $day){foreach($day['slots'] as $slot){
        $slotTimesCorrect=$slotTimesCorrect && date('Y-m-d H:i:s',$slot['starts_at'])===$slot['time'];
        $noPastAvailable=$noPastAvailable && (!$slot['is_available'] || $slot['starts_at']>$slots['server_now']);
    }}
    check($slotTimesCorrect,'All slot timestamps match local clinic time');
    check($noPastAvailable,'Schedule never marks past slots available');
    foreach($slots['days'] as $day){foreach($day['slots'] as $slot){if($slot['is_available'])$available[]=$slot['time'];}}
    $booking=['name'=>'Тест','phone'=>$phone,'pet_name'=>'Барсик','pet_type'=>'Кошка','pet_id'=>$pet,'doctor_id'=>$doctor,'service_id'=>$service,'appointment_at'=>$available[0],'client_comment'=>'Test'];
    $beforePastPets=(int)scalar('SELECT COUNT(*) FROM pets');
    $beforePastAppointments=(int)scalar('SELECT COUNT(*) FROM zapic_na_priem');
    $past=date('Y-m-d H:i:s',(int)(floor(time()/1800)*1800));
    check(!api('appointments/create.php',array_replace($booking,['appointment_at'=>$past,'pet_id'=>0,'pet_name'=>'Must not exist']))['success'],'Direct API rejects elapsed local interval');
    check((int)scalar('SELECT COUNT(*) FROM pets')===$beforePastPets && (int)scalar('SELECT COUNT(*) FROM zapic_na_priem')===$beforePastAppointments,'Past booking leaves no appointment or pet');
    check(api('appointments/create.php',$booking)['success'],'Booking still works after schema extension');
    $bookingId=(int)scalar('SELECT MAX(id) FROM zapic_na_priem');
    check(!api('appointments/create.php',$booking)['success'],'Duplicate slot rejected');
    check(api('appointments/cancel.php',['id'=>$bookingId,'phone'=>$phone])['success'],'Cancel test booking');
    check(api('appointments/create.php',$booking)['success'],'Rebook released slot');
    $newPetBooking=array_replace($booking,['pet_id'=>0,'pet_name'=>'Новый при записи','pet_pol'=>'Самка','appointment_at'=>$available[1]]);
    check(api('appointments/create.php',$newPetBooking)['success'],'Create new pet through booking');
    check(scalar("SELECT pol FROM pets WHERE client_id=$owner AND name='Новый при записи'")==='Самка','Booking stores new pet sex');
    foreach(['pred_diag','klin_prizn','ishod'] as $field) {
        $single=visit($pet,-1);
        check(!saveCard($single,[$field=>'Тест'])['success'],'Incomplete card with only one field rejected: '.$field);
    }
    $dateOnly=visit($pet,-1);
    check(!saveCard($dateOnly,['data_zabol'=>date('Y-m-d',strtotime('-2 days'))])['success'],'Date alone cannot complete a medical record');
    $newRollback=visit($pet,-1);
    $db->query("CREATE TRIGGER test_fail_new BEFORE UPDATE ON zapic_na_priem FOR EACH ROW BEGIN IF NEW.id=$newRollback THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected insert error'; END IF; END");
    $beforeFailure=(int)scalar('SELECT COUNT(*) FROM service_history');
    check(!saveCard($newRollback,$fields)['success'],'Failure after new card insert reported');
    check((int)scalar("SELECT COUNT(*) FROM medical_records WHERE id=$newRollback")===0,'New card insert rolled back');
    check(scalar("SELECT status FROM zapic_na_priem WHERE id=$newRollback")==='accepted' && (int)scalar('SELECT COUNT(*) FROM service_history')===$beforeFailure,'New card failure preserves status and history');
    $db->query('DROP TRIGGER test_fail_new');
    check($history===$db->query('SELECT * FROM service_history WHERE id='.(int)$history['id'])->fetch_assoc(),'Original history snapshot unchanged');
    $invalidCards=[
        ['diagnoz'=>'Диагноз','naz_lech'=>'Лечение'],
        ['jaloba'=>" \n ",'diagnoz'=>'Диагноз','naz_lech'=>'Лечение'],
        ['jaloba'=>'Причина','naz_lech'=>'Лечение'],
        ['jaloba'=>'Причина','diagnoz'=>' ','pred_diag'=>"\n",'procedure_done'=>'Осмотр'],
        ['jaloba'=>'Причина','diagnoz'=>'Диагноз'],
        ['jaloba'=>'Причина','pred_diag'=>'Диагноз','naz_lech'=>' ','procedure_done'=>"\n"],
        ['treatment_notes'=>'Только заметки']
    ];
    foreach($invalidCards as $i=>$card) {
        $invalid=visit($pet,-1);
        $countBefore=(int)scalar('SELECT COUNT(*) FROM service_history');
        check(!saveCard($invalid,$card)['success'],'Required medical groups rejected: '.$i);
        check((int)scalar("SELECT COUNT(*) FROM medical_records WHERE id=$invalid")===0
            && scalar("SELECT status FROM zapic_na_priem WHERE id=$invalid")==='accepted'
            && (int)scalar('SELECT COUNT(*) FROM service_history')===$countBefore,'Validation failure leaves no partial changes: '.$i);
    }
    foreach(['pred_diag','diagnoz'] as $diagnosis) {
        foreach(['naz_lech','procedure_done'] as $action) {
            $valid=visit($pet,-1);
            check(saveCard($valid,['jaloba'=>'Причина обращения',$diagnosis=>'Диагноз',$action=>'Помощь'])['success'],
                'Minimum valid medical groups: '.$diagnosis.' + '.$action);
        }
    }
    $bypass=visit($pet,-1);
    $db->query("INSERT INTO medical_records(id,notes) VALUES($bypass,'Incomplete legacy card')");
    check(!api('appointments/update_status.php',['id'=>$bypass,'status'=>'done'])['success'], 'Status API cannot bypass required medical groups');
    $insertDone=visit($pet,-1,'done');
    $insertHistory=$db->query('SELECT * FROM service_history ORDER BY id DESC LIMIT 1')->fetch_assoc();
    check($insertHistory['vid_priema']==='Первичный' && $insertHistory['address']==='Адрес на момент приёма'
        && $insertHistory['service_category']==='Test','INSERT trigger also copies extended snapshot');
    check($insertHistory['phone']===$phone && $insertHistory['weight']==='4.25','INSERT trigger copies phone and weight');
    foreach(['data_zabol','jaloba','pred_diag','diagnoz','klin_prizn','naz_lech','procedure_done','ishod','notes'] as $field)
        check($insertHistory[$field]===null,'Absent medical record stays NULL: '.$field);
    check((int)scalar("SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='service_history' AND REFERENCED_TABLE_NAME IS NOT NULL")===0,'History has no foreign keys');
    $db->query("UPDATE vladelci SET name='Изменённое имя',address='Другой адрес',phone='MUT$owner' WHERE id=$owner");
    $db->query("UPDATE pets SET name='Новая кличка',type='Другой вид',pol='Самка',age='3 года',weight=9.75 WHERE id=$pet");
    $db->query("UPDATE medical_records SET data_zabol=NULL,jaloba='Changed',pred_diag='Changed',diagnoz='Changed',klin_prizn='Changed',naz_lech='Changed',procedure_done='Changed',ishod='Changed',notes='Changed' WHERE id=$primary");
    $db->query("UPDATE services SET name='Изменённая услуга',category='Другая категория',price=9999 WHERE id=$service");
    $db->query("UPDATE admins SET name='Другое ФИО врача' WHERE id=$doctor");
    check($history===$db->query('SELECT * FROM service_history WHERE id='.(int)$history['id'])->fetch_assoc(),'Every snapshot field survives changes to owner, pet, doctor, service and medical record');
    foreach(['UPDATE service_history SET pet_name=\'Changed\' WHERE id=','DELETE FROM service_history WHERE id='] as $sql) {
        try{$db->query($sql.(int)$history['id']);check(false,'History rejects direct mutation');}
        catch(mysqli_sql_exception $e){check($e->getCode()===1644,'History rejects direct mutation');}
    }
    $db->query("DELETE m FROM medical_records m JOIN zapic_na_priem z ON z.id=m.id JOIN pets p ON p.id=z.pet_id WHERE p.client_id=$owner");
    $db->query("UPDATE zapic_na_priem z JOIN pets p ON p.id=z.pet_id SET z.perv_priem_id=NULL WHERE p.client_id=$owner");
    $db->query("DELETE z FROM zapic_na_priem z JOIN pets p ON p.id=z.pet_id WHERE p.client_id=$owner");
    $db->query("DELETE FROM pets WHERE client_id=$owner");
    $db->query("DELETE FROM vladelci WHERE id=$owner");
    $db->query("DELETE FROM services WHERE id=$service");
    check($history===$db->query('SELECT * FROM service_history WHERE id='.(int)$history['id'])->fetch_assoc(),'All copied values survive deletion of source visits, pets, owner and service');
    require __DIR__.'/workflow_checks.php';
    foreach($hashes as $t=>$hash)check($hash===hash('sha256',serialize($db->query("SELECT * FROM `$source`.`$t` ORDER BY id")->fetch_all(MYSQLI_ASSOC))),'Production data unchanged: '.$t);
} catch(Throwable $e){check(false,'Unexpected error: '.$e->getMessage());}
finally {
    if(is_resource($server)){proc_terminate($server);proc_close($server);}
    if(preg_match('/^vet_journaltest_[a-f0-9]{12}$/D',$name))$db->query("DROP DATABASE IF EXISTS `$name`");
    deleteDir($temp);
    file_put_contents(__DIR__.'/clinic_journal_results.json',json_encode(['passed'=>$passed,'failed'=>$failed,'checks'=>$checks],JSON_UNESCAPED_UNICODE|JSON_PRETTY_PRINT));
    echo "RESULT $passed passed; $failed failed\n";
}
exit($failed?1:0);
