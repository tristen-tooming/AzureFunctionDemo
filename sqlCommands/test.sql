-- TODO: Better test
INSERT INTO Emails VALUES ("1", "1", "1@gmail.com");
INSERT INTO EmailAttributes (HID, SendDate, EmailAttribute) VALUES ("1", "2020-1-1", "Test_1");

CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_2")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_3")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_4")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_5")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_6")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_7")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_8")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_9")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_10")
-- Next row returns Query
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_11")
CALL tbl_insert("2", "2", "2@gmail.com", "2020-2-2", "Test_12")
