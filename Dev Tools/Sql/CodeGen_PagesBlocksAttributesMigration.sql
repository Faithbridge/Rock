set nocount on
declare
@crlf varchar(2) = char(13) + char(10)


begin

IF OBJECT_ID('tempdb..#codeTable') IS NOT NULL
    DROP TABLE #codeTable

create table #codeTable (
    Id int identity(1,1) not null,
    CodeText nvarchar(max),
    CONSTRAINT [pk_codeTable] PRIMARY KEY CLUSTERED  ( [Id]) );
    


-- pages
    insert into #codeTable
    SELECT CONCAT('            AddPage("',
        [parentPage].[Guid], '","', 
        [p].[Name],  '","',  
        [p].[Description],  '","',
        [p].[Layout],  '","',
        [p].[Guid],  '","',
        [p].[IconCssClass], '");')
    FROM 
      [Page] p
    join [Page] [parentPage] on [p].[ParentPageId] = [parentPage].[Id]
    where [p].[IsSystem] = 0
    order by [p].[Id]

    -- block types
    insert into #codeTable
    SELECT CONCAT('            AddBlockType("',
        [Name], '","',  
        [Description], '","',  
        [Path], '","',  
        [Guid], '");')
    from [BlockType]
    where [IsSystem] = 0
    order by [Id]

    -- blocks
    insert into #codeTable
    SELECT 
        CONCAT('            AddBlock("',
        [p].[Guid], '","', 
        [bt].[Guid], '","',
        [b].[Name], '","',
        [b].[Layout], '","',
        [b].[Zone], '",',
        [b].[Order], ',"',
        [b].[Guid], '");')
    from [Block] [b]
    left outer join [Page] [p] on [p].[Id] = [b].[PageId]
    join [BlockType] [bt] on [bt].[Id] = [b].[BlockTypeId]
    where 
      [b].[IsSystem] = 0
    order by [b].[Id]

    -- attributes
    if object_id('tempdb..#attributeIds') is not null
    begin
      drop table #attributeIds
    end

    select * into #attributeIds from (select [Id] from [dbo].[Attribute] where [IsSystem] = 0) [newattribs]
    
    insert into #codeTable
    SELECT 
        CONCAT('            // Attrib for BlockType: ', bt.Name, ':', a.Name,
        @crlf ,
        '            AddBlockTypeAttribute("', 
        bt.Guid, '","',   
        ft.Guid, '","',   
        a.name, '","',  
        a.[Key], '","', 
        a.Category, '","', 
        a.Description, '",', 
        a.[Order], ',"', 
        a.DefaultValue, '","', 
        a.Guid, '");', @crlf)
    from [Attribute] [a]
    left outer join [FieldType] [ft] on [ft].[Id] = [a].[FieldTypeId]
    left outer join [BlockType] [bt] on [bt].[Id] = cast([a].[EntityTypeQualifierValue] as int)
    where EntityTypeQualifierColumn = 'BlockTypeId'
    and [a].[id] in (select [Id] from #attributeIds)

    -- attributes values    
    insert into #codeTable
    SELECT 
        CONCAT('            // Attrib Value for ', b.Name, ':', a.Name,
        @crlf ,
        '            AddBlockAttributeValue("',     
        b.Guid, '","', 
        a.Guid, '","', 
        av.Value, '");',
        @crlf   )
    from [AttributeValue] [av]
    join Block b on b.Id = av.EntityId
    join Attribute a on a.id = av.AttributeId
    where ([av].[AttributeId] in (select [Id] from #attributeIds))
    or (b.IsSystem = 0)

    drop table #attributeIds

	-- Field Types
	insert into #codeTable
	SELECT
		CONCAT(
        '            UpdateFieldType("',     
        ft.Name, '","', 
        ft.Description, '","', 
        ft.Assembly, '","', 
        ft.Class, '","', 
        ft.Guid, '");',
        @crlf   )
    from [FieldType] [ft]
    where (ft.IsSystem = 0)

    select CodeText [MigrationUp] from #codeTable order by Id
    delete from #codeTable

    -- generate MigrationDown

    insert into #codeTable SELECT CONCAT('            DeleteAttribute("', [Guid], '"); // ', [Name])  from [Attribute] where [IsSystem] = 0 and [EntityTypeQualifierColumn] = 'BlockTypeId' order by [Id] desc   
    insert into #codeTable SELECT CONCAT('            DeleteBlock("', [Guid], '"); // ', [Name]) from [Block] where [IsSystem] = 0 order by [Id] desc
    insert into #codeTable SELECT CONCAT('            DeleteBlockType("', [Guid], '"); // ', [Name]) from [BlockType] where [IsSystem] = 0 order by [Id] desc
    insert into #codeTable SELECT CONCAT('            DeletePage("', [Guid], '"); // ', [Name])  from [Page] where [IsSystem] = 0 order by [Id] desc 

    select CodeText [MigrationDown] from #codeTable order by Id

IF OBJECT_ID('tempdb..#codeTable') IS NOT NULL
    DROP TABLE #codeTable

end


