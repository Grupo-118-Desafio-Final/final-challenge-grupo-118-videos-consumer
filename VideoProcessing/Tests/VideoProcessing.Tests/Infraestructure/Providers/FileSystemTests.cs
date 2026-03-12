using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class FileSystemTests : IDisposable
{
    private readonly FileSystem _fileSystem;
    private readonly List<string> _tempFiles;
    private readonly List<string> _tempDirectories;
    private readonly ILogger<FileSystem> _logger;

    public FileSystemTests()
    {
        _logger = Substitute.For<ILogger<FileSystem>>();

        _fileSystem = new FileSystem(_logger);
        _tempFiles = new List<string>();
        _tempDirectories = new List<string>();
    }

    public void Dispose()
    {
        // Cleanup temporary directories first
        foreach (var dir in _tempDirectories.Where(Directory.Exists))
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Cleanup temporary files
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
    public void DeleteFiles_WithMultipleExistingFilesInSameDirectory_ShouldReturnTrueAndDeleteDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);

        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(tempDir, "file2.txt");
        var file3 = Path.Combine(tempDir, "file3.txt");

        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");
        File.WriteAllText(file3, "content3");

        var filePaths = new[] { file1, file2, file3 };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
        File.Exists(file1).Should().BeFalse();
        File.Exists(file2).Should().BeFalse();
        File.Exists(file3).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithNonExistentDirectory_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}");
        var file1 = Path.Combine(nonExistentDir, "file1.txt");
        var filePaths = new[] { file1 };

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
    public void DeleteFiles_WithSingleFile_ShouldDeleteDirectoryAndFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);

        var tempFile = Path.Combine(tempDir, "file.txt");
        File.WriteAllText(tempFile, "content");

        // Act
        var result = _fileSystem.DeleteFiles(new[] { tempFile });

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
        File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithNullOrEmptyPathInCollection_ShouldReturnFalse()
    {
        // Arrange
        var filePaths = new[] { "", "   ", null! };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_ShouldDeleteAllFilesAndDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);

        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(tempDir, "file2.txt");

        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        var filePaths = new[] { file1, file2 };

        // Act
        _fileSystem.DeleteFiles(filePaths);

        // Assert
        Directory.Exists(tempDir).Should().BeFalse();
        File.Exists(file1).Should().BeFalse();
        File.Exists(file2).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithLargeNumberOfFiles_ShouldHandleCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);

        var files = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var tempFile = Path.Combine(tempDir, $"file{i}.txt");
            files.Add(tempFile);
            File.WriteAllText(tempFile, $"content{i}");
        }

        // Act
        var result = _fileSystem.DeleteFiles(files);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
        files.Should().OnlyContain(f => !File.Exists(f));
    }

    [Fact]
    public void DeleteFiles_WhenExceptionOccurs_ShouldReturnFalse()
    {
        // Arrange - usa arquivo raiz do sistema de arquivos que não pode ser deletado
        var invalidPath = Path.GetPathRoot(Path.GetTempPath());
        var filePaths = new[] { invalidPath! };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithDirectoryContainingSubdirectories_ShouldDeleteEverything()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);

        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(subDir);

        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(subDir, "file2.txt");

        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        var filePaths = new[] { file1 };

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
        Directory.Exists(subDir).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_WithPathWithoutDirectory_ShouldReturnFalse()
    {
        // Arrange
        var filePaths = new[] { "filename.txt" }; // relative path without directory

        // Act
        var result = _fileSystem.DeleteFiles(filePaths);

        // Assert
        result.Should().BeFalse();
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
    public void DeleteFiles_AfterDeletingFileWithDeleteFile_ShouldStillDeleteDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);

        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(tempDir, "file2.txt");

        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        _fileSystem.DeleteFile(file1);

        // Act
        var result = _fileSystem.DeleteFiles(new[] { file2 });

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
    }

    [Fact]
    public void DeleteFiles_CalledTwiceOnSameDirectory_ShouldReturnTrueFirstTimeFalseSecondTime()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);

        var tempFile = Path.Combine(tempDir, "file.txt");
        File.WriteAllText(tempFile, "content");

        // Act
        var firstResult = _fileSystem.DeleteFiles(new[] { tempFile });
        var secondResult = _fileSystem.DeleteFiles(new[] { tempFile });

        // Assert
        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse();
    }

    #endregion
}