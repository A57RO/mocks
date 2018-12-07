using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FakeItEasy;
using FileSender.Dependencies;
using FluentAssertions;
using NUnit.Framework;

namespace FileSender
{
    public class FileSender
    {
        private readonly ICryptographer cryptographer;
        private readonly ISender sender;
        private readonly IRecognizer recognizer;

        public FileSender(
            ICryptographer cryptographer,
            ISender sender,
            IRecognizer recognizer)
        {
            this.cryptographer = cryptographer;
            this.sender = sender;
            this.recognizer = recognizer;
        }

        public Result SendFiles(File[] files, X509Certificate certificate)
        {
            return new Result
            {
                SkippedFiles = files
                    .Where(file => !TrySendFile(file, certificate))
                    .ToArray()
            };
        }

        private bool TrySendFile(File file, X509Certificate certificate)
        {
            Document document;
            if (!recognizer.TryRecognize(file, out document))
                return false;
            if (!CheckFormat(document) || !CheckActual(document))
                return false;
            var signedContent = cryptographer.Sign(document.Content, certificate);
            return sender.TrySend(signedContent);
        }

        private bool CheckFormat(Document document)
        {
            return document.Format == "4.0" ||
                   document.Format == "3.1";
        }

        private bool CheckActual(Document document)
        {
            return document.Created.AddMonths(1) > DateTime.Now;
        }

        public class Result
        {
            public File[] SkippedFiles { get; set; }
        }
    }

    //TODO: реализовать недостающие тесты
    [TestFixture]
    public class FileSender_Should
    {
        private FileSender fileSender;
        private ICryptographer cryptographer;
        private ISender sender;
        private IRecognizer recognizer;

        private Document document;
        private readonly X509Certificate certificate = new X509Certificate();
        private File file;
        private byte[] signedContent;

        [SetUp]
        public void SetUp()
        {
            // Постарайтесь вынести в SetUp всё неспецифическое конфигурирование так,
            // чтобы в конкретных тестах осталась только специфика теста,
            // без конфигурирования "обычного" сценария работы

            file = new File("someFile", new byte[] {1, 2, 3});
            signedContent = new byte[] {1, 7};

            cryptographer = A.Fake<ICryptographer>();
            sender = A.Fake<ISender>();
            recognizer = A.Fake<IRecognizer>();
            fileSender = new FileSender(cryptographer, sender, recognizer);

            document = new Document(file.Name, file.Content, DateTime.Now, "4.0");
            Document doc;
            A.CallTo(() => recognizer.TryRecognize(file, out doc))
                .Returns(true).AssignsOutAndRefParametersLazily((_) => new[] {document});
            A.CallTo(() => cryptographer.Sign(document.Content, certificate))
                .Returns(signedContent);
            A.CallTo(() => sender.TrySend(signedContent))
                .Returns(true);
        }

        [TestCase("4.0", TestName = "Good format 4.0")]
        [TestCase("3.1", TestName = "Good format 3.1")]
        public void Send_When(string format)
        {
            document = new Document(file.Name, file.Content, DateTime.Now, format);

            fileSender.SendFiles(new[] {file}, certificate)
                .SkippedFiles.Should().BeEmpty();
        }
        
        [Test]
        public void Skip_WhenBadFormat()
        {
            document = new Document(file.Name, file.Content, DateTime.Now, "000");

            fileSender.SendFiles(new[] {file}, certificate)
                .SkippedFiles.Contains(file).Should().BeTrue();            
        }

        [Test]
        public void Skip_WhenOlderThanAMonth()
        {
            document = new Document(file.Name, file.Content, DateTime.MinValue, "4.0");
            
            fileSender.SendFiles(new[] {file}, certificate)
                .SkippedFiles.Contains(file).Should().BeTrue();            
        }

        [Test]
        public void Send_WhenYoungerThanAMonth()
        {
            document = new Document(file.Name, file.Content, DateTime.Now, "4.0");            

            fileSender.SendFiles(new[] {file}, certificate)
                .SkippedFiles.Should().BeEmpty();
        }

        [Test]
        public void Skip_WhenSendFails()
        {
            A.CallTo(() => sender.TrySend(signedContent))
                .Returns(false);
            
            fileSender.SendFiles(new[] {file}, certificate)
                .SkippedFiles.Contains(file).Should().BeTrue();  
        }

        [Test]
        public void Skip_WhenNotRecognized()
        {
            Document doc;
            A.CallTo(() => recognizer.TryRecognize(A<File>.Ignored, out doc))
                .Returns(false);
                        
            fileSender.SendFiles(new[] {file}, certificate)
                .SkippedFiles.Contains(file).Should().BeTrue();
        }

        [Test]
        public void IndependentlySend_WhenSeveralFilesAndSomeAreInvalid()
        {            
            Document doc;
            var secondFile = A.Fake<File>();
            A.CallTo(() => recognizer.TryRecognize(secondFile, out doc))
                .Returns(false);
            
            fileSender.SendFiles(new[] {file, secondFile}, certificate)
                .SkippedFiles.Should().BeEquivalentTo(new [] {secondFile});
        }

        [Test]
        public void IndependentlySend_WhenSeveralFilesAndSomeCouldNotSend()
        {            
            var secondFile = A.Fake<File>();
            A.CallTo(() => sender.TrySend(signedContent))
                .ReturnsNextFromSequence(true, false);
            
            fileSender.SendFiles(new[] {file, secondFile}, certificate)
                .SkippedFiles.Should().BeEquivalentTo(new [] {secondFile});
        }
    }
}