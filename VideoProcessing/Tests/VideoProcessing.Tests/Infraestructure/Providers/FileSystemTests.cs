using FluentAssertions;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class FileSystemTests : IDisposable
{
    private readonly FileSystem _fileSystem;
    private readonly List<string> _tempFiles;

    public FileSystemTests()
    {
        _fileSystem = new FileSystem();
        _tempFiles = new List<string>();
    }

    public void Dispose()
    {
        // Cleanup temporary files created during tests
        foreach (var file in _tempFiles.Where(File.Exists))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region DeleteFile Tests

    [Fact]
    public void DeleteFile_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, "test content");

        // Act
        var result = _fileSystem.DeleteFile(tempFile);

        // Assert
        result.Should().BeTrue();
        File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.txt");

        // Act
        var result = _fileSystem.DeleteFile(nonExistentFile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_WithNullFilePath_ShouldReturnFalse()
    {
        // Act
        var result = _fileSystem.DeleteFile(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_WithEmptyFilePath_ShouldReturnFalse()
    {
        // Act
        var result = _fileSystem.DeleteFile(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_WithWhitespaceFilePath_ShouldReturnFalse()
    {
        // Act
        var result = _fileSystem.DeleteFile("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_ShouldActuallyRemoveFileFromDisk()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, "test content");
        File.Exists(tempFile).Should().BeTrue();

        // Act
        _fileSystem.DeleteFile(tempFile);

        // Assert
        File.Exists(tempFile).Should().BeFalse();
    }

    #endregion

    #region DeleteFiles Tests

    [Fact]
    public void DeleteFiles_WithMultipleExistingFiles_ShouldReturnTrue()
    {
        // Arrange
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        var file3 = Path.GetTempFileName();
        _tempFiles.AddRange(new[] { file1, file2, file3 });

        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");
        File.WriteAllText(file3, "content3");

        var filePaths = new[] { file1, file2, file3 };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeTrue();
        File.Exists(file1).Should().BeFalse();
        File.Exists(file2).Should().BeFalse();
        File.Exists(file3).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithSomeNonExistentFiles_ShouldReturnFalse()
    {
        // Arrange
        var existingFile = Path.GetTempFileName();
        _tempFiles.Add(existingFile);
        File.WriteAllText(existingFile, "content");

        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.txt");
        var filePaths = new[] { existingFile, nonExistentFile };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeFalse();
        File.Exists(existingFile).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithAllNonExistentFiles_ShouldReturnFalse()
    {
        // Arrange
        var file1 = Path.Combine(Path.GetTempPath(), $"nonexistent1-{Guid.NewGuid()}.txt");
        var file2 = Path.Combine(Path.GetTempPath(), $"nonexistent2-{Guid.NewGuid()}.txt");
        var filePaths = new[] { file1, file2 };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithNullCollection_ShouldReturnFalse()
    {
        // Act
        var result = _fileSystem.DeleteFiles(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithEmptyCollection_ShouldReturnFalse()
    {
        // Act
        var result = _fileSystem.DeleteFiles(Array.Empty<string>());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithSingleFile_ShouldWork()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, "content");

        // Act
        var result = _fileSystem.DeleteFiles(new[] { tempFile });

        // Assert
        result.Should().BeTrue();
        File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithMixedValidAndInvalidPaths_ShouldReturnFalse()
    {
        // Arrange
        var validFile = Path.GetTempFileName();
        _tempFiles.Add(validFile);
        File.WriteAllText(validFile, "content");

        var filePaths = new[] { validFile, "", "   ", null! };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeFalse();
        File.Exists(validFile).Should().BeFalse(); // Valid file should still be deleted
    }

    [Fact]
    public void DeleteFiles_ShouldDeleteAllExistingFiles()
    {
        // Arrange
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        _tempFiles.AddRange(new[] { file1, file2 });

        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        var filePaths = new[] { file1, file2 };

        // Act
        _fileSystem.DeleteFiles(filePaths);

        // Assert
        File.Exists(file1).Should().BeFalse();
        File.Exists(file2).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithLargeNumberOfFiles_ShouldHandleCorrectly()
    {
        // Arrange
        var files = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var tempFile = Path.GetTempFileName();
            _tempFiles.Add(tempFile);
            files.Add(tempFile);
            File.WriteAllText(tempFile, $"content{i}");
        }

        // Act
        var result = _fileSystem.DeleteFiles(files);

        // Assert
        result.Should().BeTrue();
        files.Should().OnlyContain(f => !File.Exists(f));
    }

    [Fact]
    public void DeleteFiles_WithPartialFailures_ShouldContinueProcessing()
    {
        // Arrange
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        _tempFiles.AddRange(new[] { file1, file2 });
        
        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");
        
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.txt");
        var filePaths = new[] { file1, nonExistentFile, file2 };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeFalse(); // Because one file doesn't exist
        File.Exists(file1).Should().BeFalse(); // But existing files are deleted
        File.Exists(file2).Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void DeleteFile_CalledTwiceOnSameFile_ShouldReturnTrueFirstTimeFalseSecondTime()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, "content");

        // Act
        var firstResult = _fileSystem.DeleteFile(tempFile);
        var secondResult = _fileSystem.DeleteFile(tempFile);

        // Assert
        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_AfterDeletingFileWithDeleteFile_ShouldReturnFalse()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, "content");
        
        _fileSystem.DeleteFile(tempFile);

        // Act
        var result = _fileSystem.DeleteFiles(new[] { tempFile });

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}