<?php
header('Access-Control-Allow-Origin: *');
const projectKey = '[PROJECT_KEY]';
$tableName = '[TABLE_NAME]';
$prevTable = '';
$prevHeaders = '';
$nextTable = '';
$nextHeaders = '';
const db_HOST = '[DB_HOST]';
const db_NAME = '[DB_NAME]';
const db_USER = '[DB_USER]';
const db_PASS = '[DB_PASS]';
const header_list = '[HEADERS]';
if (isset($_GET['key']))
{
	$tableName = (isset($_GET['table']) && $_GET['table'] != '') ? $_GET['table'] : $tableName;
	$headers = (isset($_GET['headers']) && $_GET['headers'] != '') ? explode(',',$_GET['headers']) : explode(',',header_list);
	if (isset($_GET['toggle']))
	{
		$content = Toggle($headers, $_GET['toggle']);
		die ($content);
	}
	else
	{
		$content = PullTable($headers);
		$body = '<html>
<head>
<style>
body { font-family: arial, sans-serif; }
table { border-collapse: collapse; width: 100%; }
td, th { border: 1px solid #dddddd; text-align: left; padding: 8px; }
h2 { text-align:center; width: 100%; }
h2 button { width: 200px };
tr:nth-child(even) { background-color: #dddddd; }
tr.complete { font-style: italic; font-size: 0.9em; color: #ffffff; background-color: #14CA71;}
.button { width: 40px;}
</style>
<script>
function Toggle(element) { var x = new XMLHttpRequest(); x.onreadystatechange = function() { 
	if (this.readyState == 4 && this.status == 200){
		var e = document.getElementById(element);
		e.innerHTML = this.responseText;
		e.className = (this.responseText.includes("✓"))? "" : "complete"; 
		console.log(this.responseText);}} 
	x.open("GET", "[NAME].php?key=[PROJECT_KEY]&toggle="+element+"&table='.$tableName.'"); x.send(); }
function Open(table, headers) { window.location.href = "[NAME].php?key=[PROJECT_KEY]&table="+table+"&headers="+headers; }
function Live() { window.location.href = "[NAME].php?key=[PROJECT_KEY]"; }
</script>
</head>
<body>
<h2>'.
'<button onclick="'.(($prevTable != "")?'Open(\''.$prevTable.'\',\''.$prevHeaders.'\')">':'">').$prevTable.'</button>'.
'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;'.$tableName.'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;'.
'<button onclick="'.(($nextTable != "")?'Open(\''.$nextTable.'\',\''.$nextHeaders.'\')">':'">').$nextTable.'</button>'.
'<br/><br/><button onclick="Live()">Live Feedback</button>'.
'</h2>
<table>
<tr><th class="button"></th>';
		foreach ($headers as $header)
		{
			$body.='<th>'.$header.'</th>';
		}
		$body .='</tr>';
		$body .= $content;
		$body .='</table>
</body></html>';
		die($body);
	}
}
if (!isset($_POST['key'])) Error('Missing or invalid project key!');
if (!isset($_POST['payload'])) Error('Missing data!');
try { $submission = str_replace('\\n', '<br />', $_POST['payload']); }
catch (Exception $e) {Error('Invalid json payload!');}
if (ConnectToDB()) {
	if (VerifyTables()) {
		if (StoreSubmission($submission)) {
			$mysqli->close();
			Success('submission_success', gmdate('Y-m-d H:i:s'));}
		else Error('An unkown error occured while storing submission.  Check your database permissions.');}
	else Error('An unkown error occured while creating/verifying tables.  Check your database permissions.');}
else Error('Unable to connect to database.');
function Error($text)
{
	$output = new stdClass;
	$output->success = false;
	$output->error = $text;
	die(json_encode($output));
}
function Success()
{
	$output = new stdClass;
	$output->success = true;
	$argCount = func_num_args();
	if ($argCount % 2 != 0) return;
	$args = func_get_args();
	for ($i = 0; $i < $argCount; $i += 2)
	{
		$arg = func_get_arg($i);
		$val = func_get_arg($i + 1);
		$output->$arg = $val;
	}
	die(json_encode($output));
}
$mysqli; $timestamp;
function ConnectToDB()
{
	global $mysqli, $timestamp;
	$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);
	if ($mysqli->connect_errno)
{
	error_log('Connect Error: '.$mysqli->connect_error,0);
	return false;
}
	$timestamp = date(DATE_RFC3339);
	return true;
}
function VerifyTables()
{
	global $mysqli, $timestamp, $tableName;
	if ($mysqli->query('CREATE TABLE IF NOT EXISTS '.$tableName.' (id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, Complete BOOLEAN, Submission VARCHAR(1023));')) return true;
	error_log('Verify Tables Error: '.$mysqli->error,0);
	return false;
}
function StoreSubmission($pText)
{
	global $mysqli, $timestamp, $tableName;
	$q = $mysqli->prepare('INSERT INTO '.$tableName.' (Complete, Submission) VALUES(0, ?)');
	$json = $pText;
	$q->bind_param('s', $json);
	if ($q->execute()) return true;
	error_log('Store Submission Error: '.$mysqli->error,0);
	return false;
}
function PullTable($cols)
{
	global $tableName, $prevTable, $nextTable;
	$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);
	if ($mysqli->connect_errno)
	{
		error_log('Connect Error: '.$mysqli->connect_error,0);
		die('An error occured connecting to the database...');
	}
	LoadTables($mysqli);
	$result = $mysqli->query('SELECT * FROM '.$tableName);
	if ($result->num_rows == 0) return '';
	$output = '';
	while ($row = $result->fetch_assoc())
	{
		$obj = json_decode($row['Submission']);
		$complete = $row['Complete'];
		$i = $row['id'];
		$output .= '<tr id="'.$i.'"'.(($complete == 1)? ' class="complete"':' class=""').'><td><button onclick="Toggle('.$i.');">'.(($complete == 1)? 'X':'✓').'</td>';
		foreach($cols as $header)
		{
			$output .= '<td>'.((isset($obj -> $header))? $obj -> $header:'').'</td>';
		}
		$output .= '</tr>';
	}
	$mysqli->close();
	return $output;
}
function LoadTables($sql)
{
	global $tableName, $prevTable, $nextTable, $prevHeaders, $nextHeaders;
	$tables = array_column($sql->query('SHOW TABLES')->fetch_all(),0);
	$curTable = '';
	for ($i = 0; $i < count($tables); $i++)
	{
		$sel = $tables[$i];
		if (preg_match('/FlexMk2(_\d)+_Review/i', $sel))
		{
			error_log('Tables: $prevTable, $curTable, $nextTable');
			if ($curTable == '' && $tableName != $sel) 
			{
				$prevTable = $sel;
				SetHeaders($sql, $prevTable, $prevHeaders);
			}
			else if ($tableName == $sel) $curTable = $sel;
			else if ($curTable != '' && $tableName != $sel && $nextTable == '') 
			{
				$nextTable = $sel;
				SetHeaders($sql, $nextTable, $nextHeaders);
				return;
			}
		}
	}
}
function SetHeaders($sql, $table, &$headers)
{
	$result = $sql->query('SELECT * FROM '.$table.' LIMIT 1');
	while ($row = $result->fetch_assoc()) { $headers = array_keys($row[0]); return;}
}
function Toggle($cols, $i)
{
	global $tableName;
	$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);
	if ($mysqli->connect_errno)
	{
		error_log('Connect Error: '.$mysqli->connect_error,0);
		die('An error occured connecting to the database...');
	}
	$result = $mysqli->query('SELECT * FROM '.$tableName.' WHERE id='.($i));
	if ($result->num_rows == 0) return 'Unable to find id.';
	$row = $result->fetch_assoc();
	$sub = $row['Submission'];
	$object = json_decode($sub);
	$cpl = $row['Complete'];
	$cpl = !$cpl;
	$output = '<td>'.(($row['Complete'])?'true':'false').' cpl = '.(($cpl)?'true':'false').'</td><td>'.$sub.'</td><td>'.($i).'</td>';
	if ($mysqli->query('UPDATE '.$tableName.' SET Complete='.(($cpl)?'1':'0').' WHERE id='.($i)))
	{
		$output = '<td><button onclick="Toggle('.$i.');">'.(($cpl)? 'X':'✓').'</td>';
		foreach($cols as $header)
		{
			$output .= '<td>'.((isset($object -> $header))? $object -> $header:'').'</td>';
		}
	}
	$mysqli->close();
	return $output;
}
function PullValue($pKey)
{
	global $mysqli, $tableName;
	$result = $mysqli->query('SELECT * FROM '.$tableName.' WHERE saveKey=\''.$pKey.'\'');
	if ($result->num_rows == 0) return false;
	$row = $result->fetch_assoc();
	Success('loadedVal',$row['saveVal']);
}
?>