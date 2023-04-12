<?php
header('Access-Control-Allow-Origin: *');
const projectKey = '[PROJECT_KEY]';
$live = '[LIVE]';
const db_HOST = '[DB_HOST]';
const db_NAME = '[DB_NAME]';
const db_USER = '[DB_USER]';
const db_PASS = '[DB_PASS]';
const header_list = '[HEADERS]';
if (isset($_GET['key']) && $_GET['key'] == projectKey)
{
	$headers = explode(',',header_list);
	if (isset($_GET['toggle']))
	{
		$content = Toggle($headers, $_GET['toggle']);
		die ($content);
	}
	else
	{
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
</style>
<script>
function Toggle(element) { var x = new XMLHttpRequest(); x.onreadystatechange = function() { 
	if (this.readyState == 4 && this.status == 200){
		var e = document.getElementById(element);
		e.innerHTML = this.responseText;
		e.className = (this.responseText.includes("✓"))? "" : "complete"; 
		console.log(this.responseText);}}
	x.open("GET", "[NAME].php?key=[PROJECT_KEY]&toggle="+element); x.send(); }
</script></head><body>'.$content.'</body></html>';
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
		$output .= '<h2>'.$tables[$t].'</h2><table><tr><th class="button"></th>';
		foreach ($headers as $header) { $output.='<th>'.$header.'</th>'; }
		$output .='</tr>';
		while ($row = $result->fetch_assoc())
		{
			$obj = json_decode($row['Submission']);
			$complete = $row['Complete'];
			$id = $tables[$t].'|'.$row['id'];
			$output .= '<tr id="'.$id.'"'.(($complete == 1)? ' class="complete"':' class=""').'><td><button onclick="Toggle(\''.$id.'\');">'.(($complete == 1)? 'X':'✓').'</td>';
			foreach($headers as $header){$output .= '<td>'.((isset($obj -> $header))? $obj -> $header:'').'</td>';}
			$output .= '</tr>';
		}
		$output .= '</table><br/><br/>';
	}
	$mysqli->close();
	return $output;
}
function Toggle($index)
{
	global $headers;
	$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);
	if ($mysqli->connect_errno)
	{
		error_log('Connect Error: '.$mysqli->connect_error,0);
		die('An error occured connecting to the database...');
	}
	$tableName = explode('|', $index)[0];
	$i = explode('|', $index)[1];
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
		$output = '<td><button onclick="Toggle(\''.$index.'\');">'.(($cpl)? 'X':'✓').'</td>';
		foreach($headers as $header)
		{
			$output .= '<td>'.((isset($object -> $header))? $object -> $header:'').'</td>';
		}
	}
	$mysqli->close();
	return $output;
}
?>