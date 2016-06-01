<?php
	//if(!isset($_REQUEST['playerSearch'])) {
	//    die("Search for a player!");
	//}

	$playerQuery = $_GET['playerQuery'];
	$playerSearch = strtolower($_GET['playerQuery']);
	$dbUsername = 'info344user';
	$dbPassword = 'info344pass';

	try {
		$connection = new PDO('mysql:host=info344.c8tyrcabvj5b.us-west-2.rds.amazonaws.com:3306;dbname=info344', $dbUsername, $dbPassword);
		$connection->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

		$stmt = $connection->prepare("SELECT * FROM NBA WHERE Name = '$playerSearch'");
		$stmt->execute();

		$result = $stmt->fetchAll();
		$playerData = "";
		

		if(count($result)) {
			$callback = $_GET["callback"];
			foreach($result as $row) {
				$playerData = array('name' => $row['Name'], 'team' => $row['Team'], 'GP' => $row['GP'], 'PPG' => $row['Misc_PPG'], 'FGMade' => $row['FG_Made'], 'FGAtt' => $row['FG_Att'], 'FGPct' => $row['FG_Pct'], 'ThreePtMade' => $row['3PT_Made'], 'ThreePtAtt' => $row['3PT_Att'], 'ThreePtPct' => $row['3PT_Pct'], 'FTMade' => $row['FT_Made'], 'FTAtt' => $row['FT_Att'], 'FTPct' => $row['FT_Pct'], 'ReboundsOff' => $row['Rebounds_Off'], 'ReboundsDef' => $row['Rebounds_Def'], 'MiscAst' => $row['Misc_Ast'], 'MiscTO' => $row['Misc_TO'], 'MiscStl' => $row['Misc_Stl'], 'MiscBlk' => $row['Misc_blk'], 'MiscPF' => $row['Misc_PF']);
			}
			echo $callback . '(' . json_encode($playerData) . ')';
		}
		

	} catch(PDOException $e) {
		echo 'ERROR: ' . $e->getMessage();
	}
?>