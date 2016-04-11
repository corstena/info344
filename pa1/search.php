<?php
	require_once("player.php");
	require_once("common.php");

	page_top();

	if(!isset($_REQUEST['playerSearch'])) {
	    die("Search for a player!");
	} else if(strlen($_REQUEST['playerSearch']) < 3) {
		die("Please search using at least 3 characters");
	}

	$originalSearch = $_REQUEST['playerSearch'];
	$playerSearch = strtolower($_REQUEST['playerSearch']);
	$dbUsername = 'info344user';
	$dbPassword = 'info344pass';

	echo("<h1>Search results for $originalSearch </h1>");

	try {
		$connection = new PDO('mysql:host=info344.c8tyrcabvj5b.us-west-2.rds.amazonaws.com:3306;dbname=info344', $dbUsername, $dbPassword);
		$connection->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

		$stmt = $connection->prepare("SELECT * FROM NBA WHERE Name LIKE '%$playerSearch%'");
		$stmt->execute();

		$result = $stmt->fetchAll();

		if(count($result)) {
			echo("<div id=content>");
			foreach($result as $row) {
				$newPlayer = new Player($row['Name'], $row['Team'], $row['GP'], $row['Misc_PPG'], $row['FG_Made'],$row['FG_Att'], $row['FG_Pct'], $row['3PT_Made'],$row['3PT_Att'], $row['3PT_Pct'], $row['FT_Made'], $row['FT_Att'], $row['FT_Pct'], $row['Rebounds_Off'], $row['Rebounds_Def'], $row['Misc_Ast'], $row['Misc_TO'], $row['Misc_Stl'], $row['Misc_blk'], $row['Misc_PF']);
				$newPlayer->displayPlayer();
			}
			echo("</div>");
		} else {
			echo "No rows returned.";
		}
	} catch(PDOException $e) {
		echo 'ERROR: ' . $e->getMessage();
	}
?>