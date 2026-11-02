<?php
function visitServicesReady(mysqli $conn): bool
{
    static $ready;
    if ($ready === null) {
        $row=$conn->query("SELECT
            (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE()
                AND TABLE_NAME='service_history' AND COLUMN_NAME='priem_id') AS columns_ready,
            (SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE()
                AND TRIGGER_NAME IN ('appointment_history_after_insert','appointment_history_after_update')) AS old_triggers")->fetch_assoc();
        $ready=(int)$row['columns_ready']===1 && (int)$row['old_triggers']===0;
    }
    return $ready;
}

// Shared snapshot reader: keep one appointment in API results, attach its service rows.
function visitServicesAttach(mysqli $conn, array $visits, string $idField = 'id'): array
{
    if (!$visits || !visitServicesReady($conn)) return $visits;
    $ids = array_values(array_unique(array_map(fn($v) => (int)$v[$idField], $visits)));
    $rows = $conn->query('SELECT priem_id, service_name, service_category, kol, total_price
        FROM service_history WHERE priem_id IN ('.implode(',', $ids).') ORDER BY id');
    $groups = [];
    while ($row = $rows->fetch_assoc()) {
        $id = (int)$row['priem_id'];
        unset($row['priem_id']);
        $row['kol'] = (int)$row['kol'];
        $row['total_price'] = (int)$row['total_price'];
        $groups[$id][] = $row;
    }
    foreach ($visits as &$visit) {
        $visit['performed_services'] = $groups[(int)$visit[$idField]] ?? [];
        $visit['services_total_price'] = array_sum(array_column($visit['performed_services'], 'total_price'));
    }
    return $visits;
}

function visitServicesValidate(mysqli $conn, $items): array
{
    if (!is_array($items) || !$items || count($items) > 50 || array_keys($items) !== range(0, count($items)-1)) {
        throw new InvalidArgumentException('Добавьте от 1 до 50 оказанных услуг');
    }
    $result = []; $seen = []; $sum = 0;
    $stmt = $conn->prepare('SELECT name, category, price FROM services WHERE id=? LOCK IN SHARE MODE');
    foreach ($items as $item) {
        if (!is_array($item) || is_bool($item['service_id'] ?? null) || is_bool($item['kol'] ?? null)) {
            throw new InvalidArgumentException('Неверная строка услуги');
        }
        $id = filter_var($item['service_id'] ?? null, FILTER_VALIDATE_INT, ['options'=>['min_range'=>1]]);
        $quantity = filter_var($item['kol'] ?? null, FILTER_VALIDATE_INT, ['options'=>['min_range'=>1,'max_range'=>1000]]);
        if ($id === false || $quantity === false || isset($seen[$id])) {
            throw new InvalidArgumentException('Выберите разные услуги и количество от 1 до 1000');
        }
        $stmt->bind_param('i', $id); $stmt->execute(); $service = $stmt->get_result()->fetch_assoc();
        if (!$service || (int)$service['price'] < 0) throw new InvalidArgumentException('Услуга недоступна: обновите список услуг');
        $total = (int)$service['price'] * $quantity; $sum += $total;
        if ($sum > 2147483647) throw new InvalidArgumentException('Слишком большая стоимость приёма');
        $seen[$id] = true;
        $result[] = ['name'=>$service['name'],'category'=>$service['category'],'kol'=>$quantity,'total'=>$total];
    }
    return $result;
}

// Caller holds the appointment row lock and owns the transaction.
function visitServicesSave(mysqli $conn, int $id, array $items, ?int $primary): void
{
    $stmt = $conn->prepare('SELECT v.id AS client_id,v.name AS client_name,v.phone,v.address,
        p.id AS pet_id,p.name AS pet_name,p.type AS pet_type,p.pol,p.age AS pet_age,p.weight,d.name AS doctor_name,
        m.jaloba,m.pred_diag,m.diagnoz,m.klin_prizn,m.naz_lech,m.procedure_done,m.ishod,m.notes,m.data_zabol
        FROM zapic_na_priem z JOIN pets p ON p.id=z.pet_id JOIN vladelci v ON v.id=p.client_id
        LEFT JOIN admins d ON d.id=z.doctor_id LEFT JOIN medical_records m ON m.id=z.id WHERE z.id=?');
    $stmt->bind_param('i',$id); $stmt->execute(); $source=$stmt->get_result()->fetch_assoc();
    foreach (['doctor_name','address','jaloba'] as $field) {
        if (!$source || trim($source[$field] ?? '') === '') {
            throw new InvalidArgumentException('Для завершения укажите ветеринара, адрес владельца и жалобы');
        }
    }
    $source['priem_id']=$id;
    $source['vid_priema']=$primary===null?'Первичный':'Повторный';
    $source['data_created']=$conn->query('SELECT CURRENT_TIMESTAMP')->fetch_row()[0];
    foreach ($items as $item) {
        $row=$source+['service_name'=>$item['name'],'service_category'=>$item['category'],'kol'=>$item['kol'],'total_price'=>$item['total']];
        $columns=implode(',',array_map(fn($c)=>'`'.$c.'`',array_keys($row)));
        $insert=$conn->prepare('INSERT INTO service_history ('.$columns.') VALUES ('.implode(',',array_fill(0,count($row),'?')).')');
        $insert->execute(array_values($row));
    }
}
