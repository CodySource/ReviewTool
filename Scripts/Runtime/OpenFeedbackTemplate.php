<?php
header('Access-Control-Allow-Origin: *');
$live = '[LIVE]';
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

$tableName = (isset($_GET['table']) && $_GET['table'] != '') ? $_GET['table'] : $tableName;
$headers = (isset($_GET['headers']) && $_GET['headers'] != '') ? explode(',',$_GET['headers']) : explode(',',header_list);
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
function Open(table, headers) { window.location.href = "[NAME].php?table="+table+"&headers="+headers; }
function Live() { window.location.href = "[NAME].php"; }
</script>
</head>
<body>
<h2>'.
'<button onclick="'.(($prevTable != "")?'Open(\''.$prevTable.'\',\''.$prevHeaders.'\')">':'">').$prevTable.'</button>'.
'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;'.$tableName.'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;'.
'<button onclick="'.(($nextTable != "")?'Open(\''.$nextTable.'\',\''.$nextHeaders.'\')">':'">').$nextTable.'</button>'.
'<br/><br/><button onclick="Live()"><b>Live Feedback</b><br/>('.$live.')</button>'.
'</h2>
<table>
<tr>';
foreach ($headers as $header)
{
	$body.='<th>'.$header.'</th>';
}
$body .='</tr>';
$body .= $content;
$body .='</table>
</body></html>';
die($body);

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
		$output .= '<tr id="'.$i.'"'.(($complete == 1)? ' class="complete"':' class=""').'>';
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
	global $tableName, $prevTable, $nextTable, $prevHeaders, $nextHeaders, $live;
	$tables = array_column($sql->query('SHOW TABLES LIKE \'%'.(explode('_',$live)[0]).'%Review\'')->fetch_all(),0);
	$count = count($tables);
	for ($i = 0; $i < $count; $i ++)
	{
		$vals = explode('_',$tables[$i]);
		for ($v = 0; $v < count($vals); $v ++) $vals[$v] = intval($vals[$v]) > 0 ? sprintf('%08d',$vals[$v]) : $vals[$v];
		$tables[$i] = join($vals,'_');
	}
	sort($tables);
	for ($i = 0; $i < $count; $i ++)
	{
		$vals = explode('_',$tables[$i]);
		for ($v = 0; $v < count($vals); $v ++) $vals[$v] = intval($vals[$v]) > 0 ? sprintf('%01d',$vals[$v]) : $vals[$v];
		$tables[$i] = join($vals,'_');
	}
	$curTable = '';
	for ($i = 0; $i < $count; $i++)
	{
		$sel = $tables[$i];
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
function SetHeaders($sql, $table, &$headers)
{
	$result = $sql->query('SELECT * FROM '.$table.' LIMIT 1');
	while ($row = $result->fetch_assoc()) { $headers = array_keys($row[0]); return;}
}
?>