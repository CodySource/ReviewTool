<?php
header('Access-Control-Allow-Origin: *');
$live = '[LIVE]';
const db_HOST = '[DB_HOST]';
const db_NAME = '[DB_NAME]';
const db_USER = '[DB_USER]';
const db_PASS = '[DB_PASS]';
const header_list = '[HEADERS]';
$headers = explode(',',header_list);
$content = LoadTables();
$body = '<html><head><style>
body { font-family: arial, sans-serif; }
table { border-collapse: collapse; width: 100%; }
td, th { border: 1px solid #dddddd; text-align: left; padding: 8px; }
h2 { text-align:center; width: 100%; }
h2 button { width: 200px };
tr:nth-child(even) { background-color: #dddddd; }
tr.complete { font-style: italic; font-size: 0.9em; color: #ffffff; background-color: #14CA71;}
.button { width: 40px;}
</style></head><body>'.$content.'</body></html>';
die($body);
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
function LoadTables()
{
	global $live, $headers;
	$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);
	if ($mysqli->connect_errno)
	{
		error_log('Connect Error: '.$mysqli->connect_error,0);
		die('An error occured connecting to the database...');
	}
	//	Get table names
	$tables = array_column($mysqli->query('SHOW TABLES LIKE \'%'.(explode('_',$live)[0]).'%Review\'')->fetch_all(),0);
	$count = count($tables);
	//	Edit table names for sorting
	for ($i = 0; $i < $count; $i ++)
	{
		$vals = explode('_',$tables[$i]);
		for ($v = 0; $v < count($vals); $v ++) $vals[$v] = preg_match('/^\d+$/', $vals[$v]) ? sprintf('%08d',$vals[$v]) : $vals[$v];
		$tables[$i] = join($vals,'_');
	}
	rsort($tables);
	//	Reset table names
	for ($i = 0; $i < $count; $i ++)
	{
		$vals = explode('_',$tables[$i]);
		for ($v = 0; $v < count($vals); $v ++) $vals[$v] = preg_match('/^\d+$/', $vals[$v]) ? sprintf('%01d',$vals[$v]) : $vals[$v];
		$tables[$i] = join($vals,'_');
	}
	//	Load contents
	$output = '';
	for ($t = 0; $t < $count; $t ++)
	{
		$result = $mysqli->query('SELECT * FROM '.$tables[$t]);
		if ($result->num_rows == 0) return '';
		$output .= '<h2>'.$tables[$t].'</h2><table><tr>';
		foreach ($headers as $header) { $output.='<th>'.$header.'</th>'; }
		$output .='</tr>';
		while ($row = $result->fetch_assoc())
		{
			$obj = json_decode($row['Submission']);
			$complete = $row['Complete'];
			$id = $tables[$t].'|'.$row['id'];
			$output .= '<tr id="'.$id.'"'.(($complete == 1)? ' class="complete"':' class=""').'>';
			foreach($headers as $header){$output .= '<td>'.((isset($obj -> $header))? $obj -> $header:'').'</td>';}
			$output .= '</tr>';
		}
		$output .= '</table><br/><br/>';
	}
	$mysqli->close();
	return $output;
}
?>