ALTER TABLE TF_ServiceBusMessages_Out ADD ErrorMessage NVARCHAR(MAX),
Retry INT
GO

ALTER TABLE ASB_TopicMaster ADD JsonSchema TEXT
GO

--https://app.quicktype.io/
UPDATE ASB_TopicMaster
SET JSONSCHEMA = '{
    "$schema": "http://json-schema.org/draft-06/schema#",
    "type": "array",
    "items": {
        "$ref": "#/definitions/WelcomeElement"
    },
    "definitions": {
        "WelcomeElement": {
            "type": "object",
            "additionalProperties": false,
            "properties": {
                "IHS Site Id": {
                    "type": "string",
                    "minLength": 2
                },
                "Operator Site Id": {
                    "type": "string"
                },
                "NominalLatitude": {
                    "type": "number"
                },
                "NominalLongitude": {
                    "type": "number"
                },
                "NominalAddress": {
                    "type": "string"
                },
                "Latitude": {
                    "type": "number"
                },
                "Longitude": {
                    "type": "number"
                },
                "Address": {
                    "type": "string"
                },
                "Remarks": {
                    "type": "string"
                },
                "TowerHeight": {
                    "type": "integer"
                },
                "Site Type": {
                    "type": "string"
                },
                "Region": {
                    "type": "string",
                    "minLength": 2
                },
                "State": {
                    "type": "string",
                    "minLength": 2
                },
                "Anchor Tenant": {
                    "type": "string"
                },
                "Entity": {
                    "type": "string"
                },
                "Technology": {
                    "type": "string"
                },
                "Site Configuration": {
                    "type": "string"
                },
                "OrderDate": {
                    "type": "string"
                },
                "RFIDate": {
                    "type": "string"
                },
                "OnAirDate": {
                    "type": "string"
                },
                "CreatedBy": {
                    "type": "string"
                },
                "CreatedDate": {
                    "type": "string"
                },
                "Country": {
                    "type": "string"
                },
                "RedCube_SiteId": {
                    "type": "integer"
                },
                "STATUS": {
                    "type": "string"
                }
            },
            "required": [
                "Address",
                "Anchor Tenant",
                "Country",
                "CreatedBy",
                "CreatedDate",
                "Entity",
                "IHS Site Id",
                "Latitude",
                "Longitude",
                "NominalAddress",
                "NominalLatitude",
                "NominalLongitude",
                "OnAirDate",
                "Operator Site Id",
                "OrderDate",
                "RFIDate",
                "RedCube_SiteId",
                "Region",
                "Remarks",
                "STATUS",
                "Site Configuration",
                "Site Type",
                "State",
                "Technology",
                "TowerHeight"
            ],
            "title": "WelcomeElement"
        }
    }
}'
FROM ASB_TopicMaster
WHERE TopicName = 'sitecreation'
GO

ALTER PROCEDURE dbo.TF_GET_ServiceBusMessages
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #Messages
    (
        ID INT
    )

    UPDATE O
	SET O.StatusFlag = 'R'
	OUTPUT Inserted.ID
	INTO #Messages(Id)
	FROM dbo.TF_ServiceBusMessages_Out O
        INNER JOIN dbo.ASB_TopicMaster TM ON O.Topic = TM.TopicName
	WHERE ISNULL(IsEnabled, 0) = 1
        AND ISNULL(TM.TypeOfTopic, '') = 'Send'
        AND (
			(
				ISNULL(IsRead, 0) = 0
        AND ISNULL(O.StatusFlag, '') = ''
				)
        OR (
				ISNULL(IsRead, 0) = 1
        AND ISNULL(O.StatusFlag, '') = 'F'
        AND ISNULL(O.Retry, 0) <= 5
				)
			)

    SELECT O.ID
		, O.Topic
		, O.[Message]
		, O.IsRead
		, O.StatusFlag
		, O.SentDate
		, O.CreatedDate
		, O.CreatedBy
		, TM.PrimaryKey
		, TM.SecondaryKey
		, TM.PrimaryConnectionString
		, TM.SecondaryConnectionString
		, TM.IsEnabled
		, TM.JsonSchema
    FROM dbo.TF_ServiceBusMessages_Out O WITH (NOLOCK)
        INNER JOIN #Messages M ON O.ID = M.ID
        INNER JOIN dbo.ASB_TopicMaster TM WITH (NOLOCK) ON O.Topic = TM.TopicName
    ORDER BY O.ID
		,O.Topic
END
GO
ALTER PROCEDURE dbo.TF_Update_ServiceBusMessageStatus
    (
    @ID INT
	,
    @StatusFlag NVARCHAR(10)
	,
    @Message NVARCHAR(MAX)
)
AS
BEGIN
    UPDATE A
	SET A.IsRead = 1
		,A.StatusFlag = @StatusFlag
		,A.ErrorMessage = @Message
		,A.SentDate = dbo.TF_GETDATE()
	FROM dbo.TF_ServiceBusMessages_Out A
	WHERE A.ID = @ID
END
GO

/*
"IHS Site Id":"IHS_ABJ_22593Z", SAQ
SELECT TOP 1 * FROM TF_ServiceBusMessages_Out WHERE Topic = 'sitecreation' ORDER BY 1 DESC

UPDATE TF_ServiceBusMessages_Out
SET 
[Message] = '[{"Operator Site Id":"Demo Site 2","NominalLatitude":7.000000,"NominalLongitude":7.000000,"NominalAddress":"Demo Site 2 Address","Latitude":7.000000,"Longitude":7.000000,"Address":"Demo Site 2 Address","Remarks":"OK","TowerHeight":0.000000000000000e+000,"Site Type":"GF","Region":"Abuja","State":"Kaduna","Anchor Tenant":"Airtel NG","Entity":"IHS","Technology":"2G+3g","Site Configuration":"Indoor","OrderDate":"03\/05\/2020","RFIDate":"","OnAirDate":"","CreatedBy":"Ramesh Madem","CreatedDate":"03\/05\/2020","Country":"Nigeria","RedCube_SiteId":22593,"STATUS":""}]', 
IsRead = 0,
StatusFlag = NULL,
SentDate = NULL
WHERE Id =64769

*/


/*
Dev
--"IHS Site Id":"IHS_ABJ_22593Z", SAQ
SELECT TOP 2 * FROM TF_ServiceBusMessages_Out WHERE Topic = 'sitecreation' ORDER BY 1 DESC

--"IHS Site Id":"IHS_LAG_4153B"
UPDATE TF_ServiceBusMessages_Out
SET 
[Message] = '[{"Operator Site Id":"LG1252","NominalLatitude":6.607360,"NominalLongitude":3.410200,"NominalAddress":"Mile 13, Chestnut Oil & Gas, Owode Onirin, Ikorodu Road, Lagos State.","Latitude":6.607360,"Longitude":3.410200,"Address":"Mile 13, Chestnut Oil & Gas, Owode Onirin, Ikorodu Road, Lagos State.","Remarks":"ok","TowerHeight":3.500000000000000e+001,"Site Type":"GF","Region":"Lagos","State":"Lagos","Anchor Tenant":"MTN NG","Entity":"IHS","Technology":"LTE","Site Configuration":"Indoor","OrderDate":"01\/01\/2020","RFIDate":"03\/20\/2019","OnAirDate":"04\/07\/2020","CreatedBy":"System - Phase1 - DM","CreatedDate":"11\/23\/2019","Country":"Nigeria","RedCube_SiteId":14824,"STATUS":"On Air"}]', 
IsRead = 0,
StatusFlag = NULL,
SentDate = NULL
WHERE Id =64769

UPDATE TF_ServiceBusMessages_Out
SET 
[Message] = '[{"IHS Site Id":"IHS_ABJ_22592Z","Operator Site Id":"Demo Site 1","NominalLatitude":7.000000,"NominalLongitude":77.000000,"NominalAddress":"Demo Site 1 address","Latitude":7.000000,"Longitude":77.000000,"Address":"Demo Site 1 address","Remarks":"","TowerHeight":0.000000000000000e+000,"Site Type":"","Region":"Abuja","State":"Abuja","Anchor Tenant":"Airtel NG","Entity":"IHS","Technology":"2G+3g","Site Configuration":"Indoor","OrderDate":"03\/05\/2020","RFIDate":"","OnAirDate":"","CreatedBy":"Ramesh Madem","CreatedDate":"03\/05\/2020","Country":"Nigeria","RedCube_SiteId":22592,"STATUS":"SAQ"}]', 
IsRead = 0,
StatusFlag = NULL,
SentDate = NULL
WHERE Id =64765
*/

/*
POC


--"IHS Site Id":"IHS_ABJ_22593Z", SAQ
SELECT TOP 2 * FROM TF_ServiceBusMessages_Out WHERE Topic = 'sitecreation' ORDER BY 1 DESC

--"IHS Site Id":"IHS_LAG_4153B"
UPDATE TF_ServiceBusMessages_Out
SET 
[Message] = '[{"Operator Site Id":"LG1252","NominalLatitude":6.607360,"NominalLongitude":3.410200,"NominalAddress":"Mile 13, Chestnut Oil & Gas, Owode Onirin, Ikorodu Road, Lagos State.","Latitude":6.607360,"Longitude":3.410200,"Address":"Mile 13, Chestnut Oil & Gas, Owode Onirin, Ikorodu Road, Lagos State.","Remarks":"ok","TowerHeight":3.500000000000000e+001,"Site Type":"GF","Region":"Lagos","State":"Lagos","Anchor Tenant":"MTN NG","Entity":"IHS","Technology":"LTE","Site Configuration":"Indoor","OrderDate":"01\/01\/2020","RFIDate":"03\/20\/2019","OnAirDate":"04\/07\/2020","CreatedBy":"System - Phase1 - DM","CreatedDate":"11\/23\/2019","Country":"Nigeria","RedCube_SiteId":14824,"STATUS":"On Air"}]', 
IsRead = 0,
StatusFlag = NULL,
SentDate = NULL
WHERE Id =64842

UPDATE TF_ServiceBusMessages_Out
SET 
[Message] = '[{"IHS Site Id":"IHS_LAG_4152B","Operator Site Id":"LG0119","NominalLatitude":6.674206,"NominalLongitude":3.605190,"NominalAddress":"A space of land at No 2, Owoniboys road, Aleke B\/Stop, Ikorodu, Lagos State","Latitude":6.674206,"Longitude":3.605190,"Address":"A space of land at No 2, Owoniboys road, Aleke B\/Stop, Ikorodu, Lagos State","Remarks":"ok","TowerHeight":2.500000000000000e+001,"Site Type":"GF","Region":"Lagos","State":"Lagos","Anchor Tenant":"MTN NG","Entity":"IHS","Technology":"3G","Site Configuration":"Indoor","OrderDate":"01\/01\/2020","RFIDate":"10\/29\/2018","OnAirDate":"04\/07\/2020","CreatedBy":"System - Phase1 - DM","CreatedDate":"11\/23\/2019","Country":"Nigeria","RedCube_SiteId":14823,"STATUS":"On Air"}]', 
IsRead = 0,
StatusFlag = NULL,
SentDate = NULL
WHERE Id =64838
*/
