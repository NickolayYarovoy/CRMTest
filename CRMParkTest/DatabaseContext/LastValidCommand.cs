using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMParkTest.DatabaseContext
{
    internal class LastValidCommand
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public string Text { get; set; }

        public LastValidCommand(long id, string text)
        {
            Id = id;
            Text = text;
        }
    }
}
