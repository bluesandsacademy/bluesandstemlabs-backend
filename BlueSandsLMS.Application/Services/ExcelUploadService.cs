using BlueSandsLMS.Common.DTOs;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

public class ExcelUploadService : IExcelUploadService
{
    private readonly string _connectionString;

    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sim Page"] = "SimPage",
        ["Sim String"] = "SimString",
        ["Teacher Tips Doc"] = "TeacherTipsDoc",
        ["Teacher Tips Doc (Spanish)"] = "TeacherTipsDoc",
        ["PDF"] = "PdfUrl",
        ["PDF (Spanish)"] = "PdfUrl",
        ["Physics"] = "Physics",
        ["Chemistry"] = "Chemistry",
        ["CHEMISTRY"] = "Chemistry",
        ["Earth & Space"] = "EarthSpace",
        ["EARTH & SPACE"] = "EarthSpace",
        ["Biology"] = "Biology",
        ["BIOLOGY"] = "Biology",
        ["Math & Statistics"] = "MathStatistics",
        ["Low Grade Level"] = "LowGradeLevel",
        ["High Grade Level"] = "HighGradeLevel",
        ["Main Topics"] = "MainTopics",
        ["Keywords"] = "Keywords",
        ["Description"] = "Description",
        ["Sample Learning Goals"] = "SampleLearningGoals",
        ["Translations"] = "Translations",
        ["Published"] = "Published",
        ["Runnable Resource"] = "RunnableResource",
        ["CheerpJ Runnable"] = "CheerpJRunnable",
        ["Filename"] = "Filename",
        ["Title"] = "Title",
        ["Type"] = "Type",
        ["# of Screens"] = "NumberOfScreens",
        ["Screen Names"] = "ScreenNames",
        ["SimPage"] = "SimPage",
        ["SimString"] = "SimString",
        ["TeacherTipsDoc"] = "TeacherTipsDoc",
        ["PdfUrl"] = "PdfUrl",
        ["RunnableResource"] = "RunnableResource",
        ["CheerpJRunnable"] = "CheerpJRunnable",
        ["LowGradeLevel"] = "LowGradeLevel",
        ["HighGradeLevel"] = "HighGradeLevel",
        ["MainTopics"] = "MainTopics",
        ["SampleLearningGoals"] = "SampleLearningGoals",
        ["EarthSpace"] = "EarthSpace",
        ["SimulationUrl"] = "SimulationUrl",
        ["ThumbnailUrl"] = "ThumbnailUrl",
        ["Topic"] = "Topic",
        ["GradeLevel"] = "GradeLevel",
        ["Standards"] = "Standards",
        ["LearningGoals"] = "LearningGoals",
        ["IsActive"] = "IsActive",
        ["IsFree"] = "IsFree",
        ["Id"] = "Id",
    };

    public ExcelUploadService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
    }

    //public async Task<int> UploadPhETExcelAsync(Stream fileStream)
    //{
    //    var simulations = new List<PhETSimulationExcelDTO>();

    //    using (var package = new ExcelPackage(fileStream))
    //    {
    //        var worksheet = package.Workbook.Worksheets.Count > 3
    //            ? package.Workbook.Worksheets[3]
    //            : package.Workbook.Worksheets[0];

    //        int rowCount = worksheet.Dimension.Rows;
    //        int colCount = worksheet.Dimension.Columns;

    //        // FIX: The header row is not always row 1 - this workbook has a leftover
    //        // title/link row above the real headers. Scan for the row that actually
    //        // contains "Title" and "Type" instead of assuming row 1.
    //        int headerRow = FindHeaderRow(worksheet, rowCount, colCount);

    //        // Build column map using alias resolution
    //        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    //        for (int col = 1; col <= colCount; col++)
    //        {
    //            var headerValue = worksheet.Cells[headerRow, col].Text?.Trim();
    //            if (string.IsNullOrEmpty(headerValue)) continue;

    //            var canonical = HeaderAliases.TryGetValue(headerValue, out var mapped) ? mapped : headerValue;
    //            if (!columnMap.ContainsKey(canonical))
    //                columnMap[canonical] = col;
    //        }

    //        // FIX: use the resolved Title column (not hardcoded col 1/2) to detect blank rows,
    //        // and start reading data on the row right after the real header row.
    //        columnMap.TryGetValue("Title", out int titleCol);
    //        columnMap.TryGetValue("Type", out int typeCol);

    //        for (int row = headerRow + 1; row <= rowCount; row++)
    //        {
    //            bool titleEmpty = titleCol == 0 || string.IsNullOrWhiteSpace(worksheet.Cells[row, titleCol].Text);
    //            bool typeEmpty = typeCol == 0 || string.IsNullOrWhiteSpace(worksheet.Cells[row, typeCol].Text);
    //            if (titleEmpty && typeEmpty)
    //                continue;

    //            var title = GetValue<string>(worksheet, row, columnMap, "Title") ?? string.Empty;
    //            var simPage = GetValue<string>(worksheet, row, columnMap, "SimPage");
    //            var simString = GetValue<string>(worksheet, row, columnMap, "SimString");

    //            // Generate a unique SimulationUrl if not provided in the Excel file.
    //            // The DB has a unique index on SimulationUrl, so empty/null will cause duplicates.
    //            var simulationUrl = GetValue<string>(worksheet, row, columnMap, "SimulationUrl");
    //            if (string.IsNullOrWhiteSpace(simulationUrl))
    //            {
    //                // Use SimPage if available, otherwise generate a unique URL from title
    //                simulationUrl = !string.IsNullOrWhiteSpace(simPage)
    //                    ? simPage
    //                    : $"https://phet.colorado.edu/sims/{Uri.EscapeDataString(title.ToLowerInvariant().Replace(' ', '-'))}/{Guid.NewGuid():N}";
    //            }

    //            // FIX: this workbook has no "Topic" column at all, only "Main Topics".
    //            // Fall back to MainTopics so Topic isn't always an empty string.
    //            // NOTE: confirm this is the mapping you want.
    //            var topic = GetValue<string>(worksheet, row, columnMap, "Topic");
    //            if (string.IsNullOrWhiteSpace(topic))
    //                topic = GetValue<string>(worksheet, row, columnMap, "MainTopics") ?? string.Empty;

    //            var sim = new PhETSimulationExcelDTO
    //            {
    //                Id = Guid.NewGuid(),
    //                Title = title,
    //                SimulationUrl = simulationUrl,
    //                ThumbnailUrl = GetValue<string>(worksheet, row, columnMap, "ThumbnailUrl")
    //     ?? GetValue<string>(worksheet, row, columnMap, "PreviewImage"),
    //                Topic = topic,
    //                Description = GetValue<string>(worksheet, row, columnMap, "Description")
    //     ?? GetValue<string>(worksheet, row, columnMap, "OverviewText"),
    //                LearningGoals = GetValue<string>(worksheet, row, columnMap, "LearningGoals")
    //     ?? GetValue<string>(worksheet, row, columnMap, "LearningObjectives"),
    //                GradeLevel = GetValue<string>(worksheet, row, columnMap, "GradeLevel")
    //     ?? GetValue<string>(worksheet, row, columnMap, "TargetGradeBand"),
    //                Standards = GetValue<string>(worksheet, row, columnMap, "Standards")
    //     ?? GetValue<string>(worksheet, row, columnMap, "CurriculumStandards"),
    //                Keywords = GetValue<string>(worksheet, row, columnMap, "Keywords")
    //     ?? GetValue<string>(worksheet, row, columnMap, "SearchTerms"),
    //                IsActive = true,
    //                DateCreated = DateTime.UtcNow,
    //                LastUpdated = null,
    //                Type = GetValue<string>(worksheet, row, columnMap, "Type")
    //     ?? GetValue<string>(worksheet, row, columnMap, "ResourceFormat"),
    //                NumberOfScreens = GetValue<int?>(worksheet, row, columnMap, "NumberOfScreens")
    //     ?? GetValue<int?>(worksheet, row, columnMap, "ScreenCount"),
    //                ScreenNames = GetValue<string>(worksheet, row, columnMap, "ScreenNames")
    //     ?? GetValue<string>(worksheet, row, columnMap, "AvailableScreens"),
    //                SimPage = simPage,
    //                SimString = simString,
    //                TeacherTipsDoc = GetValue<string>(worksheet, row, columnMap, "TeacherTipsDoc")
    //     ?? GetValue<string>(worksheet, row, columnMap, "InstructorGuide"),
    //                PdfUrl = GetValue<string>(worksheet, row, columnMap, "PdfUrl")
    //     ?? GetValue<string>(worksheet, row, columnMap, "GuideDocumentUrl"),
    //                RunnableResource = GetValue<string>(worksheet, row, columnMap, "RunnableResource")
    //     ?? GetValue<string>(worksheet, row, columnMap, "ExecutionResource"),
    //                CheerpJRunnable = GetValue<string>(worksheet, row, columnMap, "CheerpJRunnable")
    //     ?? GetValue<string>(worksheet, row, columnMap, "LegacyRuntimeUrl"),
    //                Filename = GetValue<string>(worksheet, row, columnMap, "Filename")
    //     ?? GetValue<string>(worksheet, row, columnMap, "ResourceFileName"),
    //                Physics = GetBoolValue(worksheet, row, columnMap, "Physics")
    //     || GetBoolValue(worksheet, row, columnMap, "SupportsPhysics"),
    //                MathStatistics = GetBoolValue(worksheet, row, columnMap, "MathStatistics")
    //     || GetBoolValue(worksheet, row, columnMap, "SupportsMathematics"),
    //                Chemistry = GetBoolValue(worksheet, row, columnMap, "Chemistry")
    //     || GetBoolValue(worksheet, row, columnMap, "SupportsChemistry"),
    //                EarthSpace = GetBoolValue(worksheet, row, columnMap, "EarthSpace")
    //     || GetBoolValue(worksheet, row, columnMap, "SupportsEarthScience"),
    //                Biology = GetBoolValue(worksheet, row, columnMap, "Biology")
    //     || GetBoolValue(worksheet, row, columnMap, "SupportsBiology"),
    //                LowGradeLevel = GetValue<string>(worksheet, row, columnMap, "LowGradeLevel")
    //     ?? GetValue<string>(worksheet, row, columnMap, "MinimumGradeBand"),
    //                HighGradeLevel = GetValue<string>(worksheet, row, columnMap, "HighGradeLevel")
    //     ?? GetValue<string>(worksheet, row, columnMap, "MaximumGradeBand"),
    //                MainTopics = GetValue<string>(worksheet, row, columnMap, "MainTopics")
    //     ?? GetValue<string>(worksheet, row, columnMap, "PrimaryConcepts"),
    //                SampleLearningGoals = GetValue<string>(worksheet, row, columnMap, "SampleLearningGoals")
    //     ?? GetValue<string>(worksheet, row, columnMap, "ExampleObjectives"),
    //                Translations = GetValue<string>(worksheet, row, columnMap, "Translations")
    //     ?? GetValue<string>(worksheet, row, columnMap, "SupportedLanguages"),
    //                Published = GetValue<string>(worksheet, row, columnMap, "Published")
    //     ?? GetValue<string>(worksheet, row, columnMap, "PublicationDate"),
    //                IsFree = true
    //            };

    //            simulations.Add(sim);
    //        }
    //    }

    //    return await SaveToDatabaseAsync(simulations);
    //}


    public async Task<int> UploadPhETExcelAsync(Stream fileStream)
    {
        var simulations = new List<PhETSimulationExcelDTO>();

        using (var package = new ExcelPackage(fileStream))
        {
            var worksheet = package.Workbook.Worksheets.Count > 3
                ? package.Workbook.Worksheets[3]
                : package.Workbook.Worksheets[0];

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            // Find the header row
            int headerRow = FindHeaderRow(worksheet, rowCount, colCount);

            // Build column map using alias resolution
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= colCount; col++)
            {
                var headerValue = worksheet.Cells[headerRow, col].Text?.Trim();
                if (string.IsNullOrEmpty(headerValue)) continue;

                var canonical = HeaderAliases.TryGetValue(headerValue, out var mapped) ? mapped : headerValue;
                if (!columnMap.ContainsKey(canonical))
                    columnMap[canonical] = col;
            }

            columnMap.TryGetValue("Title", out int titleCol);
            columnMap.TryGetValue("Type", out int typeCol);

            for (int row = headerRow + 1; row <= rowCount; row++)
            {
                bool titleEmpty = titleCol == 0 || string.IsNullOrWhiteSpace(worksheet.Cells[row, titleCol].Text);
                bool typeEmpty = typeCol == 0 || string.IsNullOrWhiteSpace(worksheet.Cells[row, typeCol].Text);
                if (titleEmpty && typeEmpty)
                    continue;

                var title = GetValue<string>(worksheet, row, columnMap, "Title") ?? string.Empty;
                var simPage = GetValue<string>(worksheet, row, columnMap, "SimPage");
                var simString = GetValue<string>(worksheet, row, columnMap, "SimString");

                var simulationUrl = GetValue<string>(worksheet, row, columnMap, "SimulationUrl");
                if (string.IsNullOrWhiteSpace(simulationUrl))
                {
                    simulationUrl = !string.IsNullOrWhiteSpace(simPage)
                        ? simPage
                        : $"https://phet.colorado.edu/sims/{Uri.EscapeDataString(title.ToLowerInvariant().Replace(' ', '-'))}/{Guid.NewGuid():N}";
                }

                var topic = GetValue<string>(worksheet, row, columnMap, "Topic");
                if (string.IsNullOrWhiteSpace(topic))
                    topic = GetValue<string>(worksheet, row, columnMap, "MainTopics") ?? string.Empty;

                // Get and truncate Translations if needed (database column max length)
                var translations = GetValue<string>(worksheet, row, columnMap, "Translations")
                    ?? GetValue<string>(worksheet, row, columnMap, "SupportedLanguages")
                    ?? string.Empty;

                // Truncate translations to 2000 characters if the DB column is NVARCHAR(2000)
                // Adjust the max length based on your actual column definition
                const int maxTranslationsLength = 2000;
                if (translations.Length > maxTranslationsLength)
                {
                    // Truncate at the last complete language entry
                    var truncated = translations.Substring(0, maxTranslationsLength - 50);
                    var lastSemicolon = truncated.LastIndexOf(';');
                    if (lastSemicolon > 0)
                    {
                        translations = truncated.Substring(0, lastSemicolon + 1) + "... (truncated)";
                    }
                    else
                    {
                        translations = truncated + "... (truncated)";
                    }
                }

                // Also truncate other potentially long fields
                var description = GetValue<string>(worksheet, row, columnMap, "Description")
                    ?? GetValue<string>(worksheet, row, columnMap, "OverviewText")
                    ?? string.Empty;
                if (description.Length > 4000)
                    description = description.Substring(0, 3997) + "...";

                var learningGoals = GetValue<string>(worksheet, row, columnMap, "LearningGoals")
                    ?? GetValue<string>(worksheet, row, columnMap, "LearningObjectives")
                    ?? string.Empty;
                if (learningGoals.Length > 4000)
                    learningGoals = learningGoals.Substring(0, 3997) + "...";

                var teacherTipsDoc = GetValue<string>(worksheet, row, columnMap, "TeacherTipsDoc")
                    ?? GetValue<string>(worksheet, row, columnMap, "InstructorGuide")
                    ?? string.Empty;
                if (teacherTipsDoc.Length > 2000)
                    teacherTipsDoc = teacherTipsDoc.Substring(0, 1997) + "...";

                var sim = new PhETSimulationExcelDTO
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    SimulationUrl = simulationUrl,
                    ThumbnailUrl = GetValue<string>(worksheet, row, columnMap, "ThumbnailUrl")
                        ?? GetValue<string>(worksheet, row, columnMap, "PreviewImage"),
                    Topic = topic,
                    Description = description,
                    LearningGoals = learningGoals,
                    GradeLevel = GetValue<string>(worksheet, row, columnMap, "GradeLevel")
                        ?? GetValue<string>(worksheet, row, columnMap, "TargetGradeBand"),
                    Standards = GetValue<string>(worksheet, row, columnMap, "Standards")
                        ?? GetValue<string>(worksheet, row, columnMap, "CurriculumStandards"),
                    Keywords = GetValue<string>(worksheet, row, columnMap, "Keywords")
                        ?? GetValue<string>(worksheet, row, columnMap, "SearchTerms"),
                    IsActive = true,
                    DateCreated = DateTime.UtcNow,
                    LastUpdated = null,
                    Type = GetValue<string>(worksheet, row, columnMap, "Type")
                        ?? GetValue<string>(worksheet, row, columnMap, "ResourceFormat"),
                    NumberOfScreens = GetValue<int?>(worksheet, row, columnMap, "NumberOfScreens")
                        ?? GetValue<int?>(worksheet, row, columnMap, "ScreenCount"),
                    ScreenNames = GetValue<string>(worksheet, row, columnMap, "ScreenNames")
                        ?? GetValue<string>(worksheet, row, columnMap, "AvailableScreens"),
                    SimPage = simPage,
                    SimString = simString,
                    TeacherTipsDoc = teacherTipsDoc,
                    PdfUrl = GetValue<string>(worksheet, row, columnMap, "PdfUrl")
                        ?? GetValue<string>(worksheet, row, columnMap, "GuideDocumentUrl"),
                    RunnableResource = GetValue<string>(worksheet, row, columnMap, "RunnableResource")
                        ?? GetValue<string>(worksheet, row, columnMap, "ExecutionResource"),
                    CheerpJRunnable = GetValue<string>(worksheet, row, columnMap, "CheerpJRunnable")
                        ?? GetValue<string>(worksheet, row, columnMap, "LegacyRuntimeUrl"),
                    Filename = GetValue<string>(worksheet, row, columnMap, "Filename")
                        ?? GetValue<string>(worksheet, row, columnMap, "ResourceFileName"),
                    Physics = GetBoolValue(worksheet, row, columnMap, "Physics")
                        || GetBoolValue(worksheet, row, columnMap, "SupportsPhysics"),
                    MathStatistics = GetBoolValue(worksheet, row, columnMap, "MathStatistics")
                        || GetBoolValue(worksheet, row, columnMap, "SupportsMathematics"),
                    Chemistry = GetBoolValue(worksheet, row, columnMap, "Chemistry")
                        || GetBoolValue(worksheet, row, columnMap, "SupportsChemistry"),
                    EarthSpace = GetBoolValue(worksheet, row, columnMap, "EarthSpace")
                        || GetBoolValue(worksheet, row, columnMap, "SupportsEarthScience"),
                    Biology = GetBoolValue(worksheet, row, columnMap, "Biology")
                        || GetBoolValue(worksheet, row, columnMap, "SupportsBiology"),
                    LowGradeLevel = GetValue<string>(worksheet, row, columnMap, "LowGradeLevel")
                        ?? GetValue<string>(worksheet, row, columnMap, "MinimumGradeBand"),
                    HighGradeLevel = GetValue<string>(worksheet, row, columnMap, "HighGradeLevel")
                        ?? GetValue<string>(worksheet, row, columnMap, "MaximumGradeBand"),
                    MainTopics = GetValue<string>(worksheet, row, columnMap, "MainTopics")
                        ?? GetValue<string>(worksheet, row, columnMap, "PrimaryConcepts"),
                    SampleLearningGoals = GetValue<string>(worksheet, row, columnMap, "SampleLearningGoals")
                        ?? GetValue<string>(worksheet, row, columnMap, "ExampleObjectives"),
                    Translations = translations,
                    Published = GetValue<string>(worksheet, row, columnMap, "Published")
                        ?? GetValue<string>(worksheet, row, columnMap, "PublicationDate"),
                    IsFree = true
                };

                simulations.Add(sim);
            }
        }

        return await SaveToDatabaseAsync(simulations);
    }

    // Helper method to safely truncate strings
    private string TruncateString(string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// FIX: Finds the row that actually contains the column headers instead of assuming
    /// it's always row 1. Scans the first few rows for one that has both "Title" and
    /// "Type" cells, which is the real header row in this workbook (row 1 is a stray
    /// "Original Link (Hyperlink)" cell left over from formatting).
    /// </summary>
    private int FindHeaderRow(ExcelWorksheet worksheet, int rowCount, int colCount, int maxRowsToScan = 5)
    {
        int scanLimit = Math.Min(maxRowsToScan, rowCount);
        for (int row = 1; row <= scanLimit; row++)
        {
            bool hasTitle = false;
            bool hasType = false;

            for (int col = 1; col <= colCount; col++)
            {
                var text = worksheet.Cells[row, col].Text?.Trim();
                if (string.IsNullOrEmpty(text)) continue;

                if (text.Equals("Title", StringComparison.OrdinalIgnoreCase)) hasTitle = true;
                else if (text.Equals("Type", StringComparison.OrdinalIgnoreCase)) hasType = true;
            }

            if (hasTitle && hasType) return row;
        }

        throw new InvalidOperationException(
            $"Could not locate the header row (expected a row containing 'Title' and 'Type') within the first {scanLimit} rows.");
    }

    private bool GetBoolValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out int colIndex))
            return false;

        var cellValue = worksheet.Cells[row, colIndex].Text?.Trim();
        if (string.IsNullOrWhiteSpace(cellValue) || cellValue.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            return false;

        if (cellValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
            cellValue.Equals("YES", StringComparison.OrdinalIgnoreCase) ||
            cellValue.Equals("1", StringComparison.Ordinal) ||
            cellValue.Equals("X", StringComparison.OrdinalIgnoreCase))
            return true;

        if (cellValue.Equals("FALSE", StringComparison.OrdinalIgnoreCase) ||
            cellValue.Equals("NO", StringComparison.OrdinalIgnoreCase) ||
            cellValue.Equals("0", StringComparison.Ordinal))
            return false;

        // Non-empty, non-recognized text (e.g. "PHYSICS", "CHEMISTRY" tags used as flags
        // in this sheet) is treated as truthy, matching the original behavior.
        return true;
    }

    private T? GetValue<T>(ExcelWorksheet worksheet, int row, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out int colIndex))
            return default;

        var cellValue = worksheet.Cells[row, colIndex].Value;
        if (cellValue == null)
            return default;

        try
        {
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (targetType == typeof(Guid))
                return (T)(object)Guid.Parse(cellValue.ToString()!);

            if (targetType == typeof(bool))
            {
                var str = cellValue.ToString()?.Trim() ?? "";
                if (str.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("YES", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("1", StringComparison.Ordinal))
                    return (T)(object)true;
                return (T)(object)false;
            }

            return (T)Convert.ChangeType(cellValue, targetType);
        }
        catch
        {
            return default;
        }
    }

    private async Task<int> SaveToDatabaseAsync(IEnumerable<PhETSimulationExcelDTO> data)
    {
        // Use MERGE to skip duplicates based on SimulationUrl unique index
        const string sql = @"
            MERGE [dbo].[PhETSimulations] AS target
            USING (SELECT @Id AS Id, @SimulationUrl AS SimulationUrl) AS source
            ON target.[SimulationUrl] = source.[SimulationUrl]
            WHEN NOT MATCHED THEN
                INSERT (
                    [Id], [Title], [SimulationUrl], [ThumbnailUrl], [Topic], [Description], 
                    [LearningGoals], [GradeLevel], [Standards], [Keywords], [IsActive], 
                    [DateCreated], [LastUpdated], [Type], [NumberOfScreens], [ScreenNames], 
                    [SimPage], [SimString], [TeacherTipsDoc], [PdfUrl], [RunnableResource], 
                    [CheerpJRunnable], [Filename], [Physics], [MathStatistics], [Chemistry], 
                    [EarthSpace], [Biology], [LowGradeLevel], [HighGradeLevel], [MainTopics], 
                    [SampleLearningGoals], [Translations], [Published], [IsFree]
                ) VALUES (
                    @Id, @Title, @SimulationUrl, @ThumbnailUrl, @Topic, @Description, 
                    @LearningGoals, @GradeLevel, @Standards, @Keywords, @IsActive, 
                    @DateCreated, @LastUpdated, @Type, @NumberOfScreens, @ScreenNames, 
                    @SimPage, @SimString, @TeacherTipsDoc, @PdfUrl, @RunnableResource, 
                    @CheerpJRunnable, @Filename, @Physics, @MathStatistics, @Chemistry, 
                    @EarthSpace, @Biology, @LowGradeLevel, @HighGradeLevel, @MainTopics, 
                    @SampleLearningGoals, @Translations, @Published, @IsFree
                )
            WHEN MATCHED THEN
                UPDATE SET
                    [Title] = @Title,
                    [Description] = @Description,
                    [Keywords] = @Keywords,
                    [Type] = @Type,
                    [NumberOfScreens] = @NumberOfScreens,
                    [ScreenNames] = @ScreenNames,
                    [SimPage] = @SimPage,
                    [SimString] = @SimString,
                    [TeacherTipsDoc] = @TeacherTipsDoc,
                    [PdfUrl] = @PdfUrl,
                    [RunnableResource] = @RunnableResource,
                    [CheerpJRunnable] = @CheerpJRunnable,
                    [Filename] = @Filename,
                    [Physics] = @Physics,
                    [MathStatistics] = @MathStatistics,
                    [Chemistry] = @Chemistry,
                    [EarthSpace] = @EarthSpace,
                    [Biology] = @Biology,
                    [LowGradeLevel] = @LowGradeLevel,
                    [HighGradeLevel] = @HighGradeLevel,
                    [MainTopics] = @MainTopics,
                    [SampleLearningGoals] = @SampleLearningGoals,
                    [Translations] = @Translations,
                    [Published] = @Published,
                    [LastUpdated] = GETUTCDATE()
            ;";

        using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, data);
    }
}

public interface IExcelUploadService
{
    Task<int> UploadPhETExcelAsync(Stream fileStream);
}