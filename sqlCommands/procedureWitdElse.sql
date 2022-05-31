-- With ELSE
-- Not in use
-- Run SQLInitCommands 
CREATE DEFINER=`root`@`localhost` PROCEDURE `emailDemo`.`tbl_insert`(
	in _HID VARCHAR(255),
	in _SenderKey VARCHAR(255),
	in _Email VARCHAR(255),
	in _SendDate Date,
	in _EmailAttribute VARCHAR(255)
)
BEGIN
		DECLARE attribute_count INT;
	
		INSERT IGNORE INTO Emails VALUES (_HID, _SenderKey, _Email);
		INSERT IGNORE INTO EmailAttributes VALUES (_HID, _SendDate, _EmailAttribute);
	
	  	SELECT count(*) FROM EmailAttributes WHERE HID = _HID and SendDate = _SendDate INTO @attribute_count;
	  
	  	IF (@attribute_count = 10) THEN
	  		SELECT EmailAttribute FROM EmailAttributes WHERE HID = _HID and SendDate = _SendDate;
	  	
	  	ELSE
	  	 	SELECT @attribute_count;
	  	END IF;
	
	END