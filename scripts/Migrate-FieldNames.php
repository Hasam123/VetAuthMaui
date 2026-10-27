<?php
if (PHP_SAPI !== 'cli') { http_response_code(404); exit; }
mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT);
function migrateFieldNames(mysqli $db): void {
    $triggers=[];
    foreach (['appointment_history_after_insert','appointment_history_after_update'] as $name) {
        $trigger=$db->query("SHOW CREATE TRIGGER `$name`")->fetch_assoc();
        $sql=$trigger['SQL Original Statement'];
        $sql=preg_replace('/^CREATE /','CREATE OR REPLACE ', $sql);
        $triggers[]=str_replace('performed_at','data_created',$sql);
    }
    foreach ([['zapic_na_priem','comment','client_comment','TEXT NOT NULL'],
        ['zapic_na_priem','created_at','data_created','DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP'],
        ['medical_records','created_at','data_created','DATETIME NULL DEFAULT CURRENT_TIMESTAMP'],
        ['service_history','performed_at','data_created','DATETIME NOT NULL']] as [$table,$old,$new,$definition]) {
        $columns=array_column($db->query("SHOW COLUMNS FROM `$table`")->fetch_all(MYSQLI_ASSOC),'Field');
        if(in_array($new,$columns,true) && !in_array($old,$columns,true)) { continue; }
        if(!in_array($old,$columns,true) || in_array($new,$columns,true)) { throw new RuntimeException("Unexpected schema: $table.$old"); }
        $db->query("ALTER TABLE `$table` CHANGE COLUMN `$old` `$new` $definition");
    }
    foreach($triggers as $sql) { $db->query($sql); }
}
if(realpath($_SERVER['SCRIPT_FILENAME'])===__FILE__) {
    $name=$argv[1]??'vet_clinic';
    if(!preg_match('/^[a-zA-Z0-9_]+$/D',$name)) { throw new RuntimeException('Invalid database'); }
    $db=new mysqli('localhost','root','',$name); $db->set_charset('utf8mb4');
    migrateFieldNames($db);
    echo "Fields and history triggers renamed successfully\n";
}
