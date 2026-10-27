<?php
// CLI only. Keep both original tables as archives, including unlinked treatment.
if (PHP_SAPI !== 'cli') { http_response_code(404); exit; }
mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT);

function migrateMedicalRecords(mysqli $db): array {
    foreach (['medical_records', 'result_obsled_legacy', 'lechenie_legacy'] as $table) {
        $r = $db->query("SHOW TABLES LIKE '$table'");
        if ($r->num_rows) { throw new RuntimeException("Migration already applied or incomplete: $table exists"); }
    }
    $db->query(file_get_contents(__DIR__ . '/../database/migrations/20261027_medical_records.sql'));
    $db->begin_transaction();
    try {
        // Older exam results are preserved in notes. Unknown creation dates stay NULL;
        // a historical treatment date has day precision only (midnight).
        $db->query("INSERT INTO medical_records (id, jaloba, diagnoz, naz_lech, procedure_done, notes, created_at)
            SELECT z.id, r.jaloba, r.diagnoz, l.naz_lech, l.procedure_done,
                NULLIF(CONCAT_WS(CHAR(10,10), NULLIF(l.notes, ''),
                    CASE WHEN COALESCE(r.result, '') <> ''
                    THEN CONCAT('Результат обследования (до объединения): ', r.result) END), ''),
                CASE WHEN l.data IS NOT NULL AND l.data <> '0000-00-00' THEN TIMESTAMP(l.data) ELSE NULL END
            FROM zapic_na_priem z
            LEFT JOIN result_obsled r ON r.zapic_id = z.id
            LEFT JOIN lechenie l ON l.zapic_id = z.id
            WHERE r.id IS NOT NULL OR l.id IS NOT NULL");
        $expected = (int)$db->query('SELECT COUNT(*) FROM (SELECT zapic_id FROM result_obsled UNION SELECT zapic_id FROM lechenie WHERE zapic_id IS NOT NULL) x')->fetch_row()[0];
        $actual = (int)$db->query('SELECT COUNT(*) FROM medical_records')->fetch_row()[0];
        if ($expected !== $actual) { throw new RuntimeException('Medical record count mismatch'); }
        $orphans = (int)$db->query('SELECT COUNT(*) FROM lechenie WHERE zapic_id IS NULL')->fetch_row()[0];
        $db->commit();
    } catch (Throwable $e) {
        $db->rollback();
        $db->query('DROP TABLE medical_records');
        throw $e;
    }
    // One atomic rename, after copying and validating. Never delete legacy data.
    $db->query('RENAME TABLE result_obsled TO result_obsled_legacy, lechenie TO lechenie_legacy');
    return ['medical_records' => $actual, 'archived_unlinked_treatments' => $orphans];
}

if (realpath($_SERVER['SCRIPT_FILENAME']) === __FILE__) {
    $name = $argv[1] ?? 'vet_clinic';
    if (!preg_match('/^[a-zA-Z0-9_]+$/D', $name)) { throw new RuntimeException('Invalid database'); }
    $db = new mysqli('localhost', 'root', '', $name);
    $db->set_charset('utf8mb4');
    echo json_encode(migrateMedicalRecords($db), JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT) . PHP_EOL;
}
