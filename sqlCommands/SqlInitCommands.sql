-- Tables
CREATE TABLE Emails (
    SenderKey VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    PRIMARY KEY (SenderKey)
)

CREATE Table EmailAttributes (
	SenderKey VARCHAR(255) NOT NULL,
    SendDate Date NOT NULL,
    EmailAttribute VARCHAR(255),
    CONSTRAINT FK_HID FOREIGN KEY (SenderKey) REFERENCES Emails(SenderKey)
)

-- Index
CREATE UNIQUE INDEX idx_email_attributes_all on EmailAttributes(SenderKey, SendDate, EmailAttribute);
	
-- Procedure for inserting data and counting email attributes.
delimiter //
DROP PROCEDURE IF EXISTS tbl_insert //

CREATE DEFINER=`root`@`localhost` PROCEDURE `emailDemo`.`tbl_insert`(
	in _SenderKey VARCHAR(255),
	in _Email VARCHAR(255),
	in _SendDate Date,
	in _EmailAttribute VARCHAR(255)
)

	BEGIN
		DECLARE attribute_count INT;
	
		-- Index would return SQL Error [1062] [230000]
		INSERT IGNORE INTO Emails VALUES (_SenderKey, _Email);
		INSERT INTO EmailAttributes VALUES (_SenderKey, _SendDate, _EmailAttribute);
		
		-- Counter for attributes
	  	SELECT count(*)
	  	FROM EmailAttributes
	  	WHERE SenderKey = _SenderKey and DATE(SendDate) = DATE(_SendDate)
	  	ORDER BY UNIX_TIMESTAMP(SendDate) DESC
	  	INTO @attribute_count;
	  
	  	-- Return data if 10 attributes
	  	IF (@attribute_count = 10) THEN
	  		SELECT EmailAttribute FROM EmailAttributes WHERE SenderKey = _SenderKey and DATE(SendDate) = DATE(_SendDate);
	  	END IF;
	  
	END //

delimiter ;
	
