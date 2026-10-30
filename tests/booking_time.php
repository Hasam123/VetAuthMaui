<?php
date_default_timezone_set('Europe/Berlin');
require __DIR__.'/../api/includes/doctor_booking.php';
$passed=0;$failed=0;
function clockCheck(bool $ok,string $label): void {global $passed,$failed;$ok?$passed++:$failed++;echo ($ok?'PASS ':'FAIL ').$label.PHP_EOL;}
clockCheck(date_default_timezone_get()==='Europe/Chisinau','Clinic overrides Berlin timezone');
$now=new DateTimeImmutable('2026-10-30T17:28:00+02:00');
$cases=[
 ['2026-10-30 16:30:00',false,'Past hour previously accepted with Berlin timezone'],
 ['2026-10-30 17:00:00',false,'Past half-hour'],
 ['2026-10-30 17:30:00',true,'Next local interval'],
 ['2026-10-30 18:00:00',true,'Last interval'],
 ['2026-10-30 18:30:00',false,'Outside opening hours'],
 ['2026-10-29 18:00:00',false,'Previous day'],
 ['2026-10-31 08:00:00',true,'Next day'],
 ['2026-11-03 18:00:00',true,'Fifth day'],
 ['2026-11-04 08:00:00',false,'Outside five-day window'],
 ['2026-10-31 07:30:00',false,'Before opening'],
 ['2026-10-31 08:15:00',false,'Invalid interval alignment'],
 ['2026-10-31 08:30:01',false,'Non-zero seconds'],
 ['2026-02-30 09:00:00',false,'Invalid calendar date'],
 ['2026-10-31T09:00:00+02:00',false,'Unexpected datetime format']
];
foreach($cases as [$value,$expected,$label])clockCheck(bookingTimeIsValid($value,$now)===$expected,$label);
clockCheck(!bookingTimeIsValid('2026-10-30 17:30:00',new DateTimeImmutable('2026-10-30T17:30:00+02:00')),'Exactly current interval is unavailable');
clockCheck(!bookingTimeIsValid('2026-10-30 17:30:00',new DateTimeImmutable('2026-10-30T17:30:01+02:00')),'Interval expires while form stays open');
clockCheck(bookingTimeIsValid('2026-10-30 17:30:00',new DateTimeImmutable('2026-10-30T15:28:00Z')),'UTC clock gives same local result');
clockCheck(bookingTimeIsValid('2026-07-10 17:30:00',new DateTimeImmutable('2026-07-10T14:28:00Z')),'Summer UTC+3');
clockCheck(!bookingTimeIsValid('2026-07-10 17:00:00',new DateTimeImmutable('2026-07-10T14:28:00Z')),'Summer past slot rejected');
clockCheck(bookingTimeIsValid('2026-10-25 08:00:00',new DateTimeImmutable('2026-10-24T23:30:00+03:00')),'Autumn clock transition next morning');
clockCheck(bookingTimeIsValid('2026-03-29 08:00:00',new DateTimeImmutable('2026-03-28T23:30:00+02:00')),'Spring clock transition next morning');
echo "RESULT $passed passed; $failed failed\n";exit($failed?1:0);
