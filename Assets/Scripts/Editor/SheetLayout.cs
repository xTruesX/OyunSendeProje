using System.Text.RegularExpressions;
using Boruhan.Animations;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Reads a sprite sheet's layout from its file name, so sheets can just be dropped into a folder.
    /// The name says what it is, how many directions (rows) and frames (columns) and the frame time, e.g.
    /// "Yürüme • 4 yön × 4 kare • 120 ms", "Bekle_4yon_3kare_240ms", "Dönüş • 8 yön • 100 ms".
    /// 4 rows are front, back, right, left; 1 row is used for every direction. Dönüş is one row of
    /// facings going round the character.
    /// </summary>
    public class SheetLayout
    {
        public AnimState state;
        public int columns;
        public AnimDirection[] rows;
        public float fps;
        public bool loop;

        public static readonly AnimDirection[] FourWay = { AnimDirection.Down, AnimDirection.Up, AnimDirection.Right, AnimDirection.Left };

        /// <summary>null if the name doesn't say how many frames or directions it has.</summary>
        public static SheetLayout FromFileName(string fileName)
        {
            var name = Simplify(fileName);
            var state = StateOf(name);
            if (state == null)
                return null;

            var dirs = Regex.Match(name, @"(\d+)\s*_?yon");
            var frames = Regex.Match(name, @"(\d+)\s*_?kare");
            var ms = Regex.Match(name, @"(\d+)\s*_?ms");
            if (!dirs.Success && !frames.Success)
                return null;

            var layout = new SheetLayout
            {
                state = state.Value,
                fps = ms.Success && int.Parse(ms.Groups[1].Value) > 0 ? 1000f / int.Parse(ms.Groups[1].Value) : 8f,
                loop = state is AnimState.Idle or AnimState.Walk or AnimState.Stagger,
            };
            int directions = dirs.Success ? int.Parse(dirs.Groups[1].Value) : 1;
            if (state == AnimState.Turn)
            {
                // One row with a frame per facing.
                layout.columns = frames.Success ? int.Parse(frames.Groups[1].Value) : directions;
                layout.rows = new[] { AnimDirection.Any };
                return layout;
            }
            if (!frames.Success)
                return null;
            layout.columns = int.Parse(frames.Groups[1].Value);
            layout.rows = directions == 4 ? FourWay : new[] { AnimDirection.Any };
            return layout;
        }

        static AnimState? StateOf(string name)
        {
            if (name.Contains("donus")) return AnimState.Turn;
            if (name.Contains("bekle")) return AnimState.Idle;
            if (name.Contains("yuru")) return AnimState.Walk;
            if (name.Contains("agir")) return AnimState.HeavyAttack;
            if (name.Contains("saldir")) return AnimState.Attack;
            if (name.Contains("kacin")) return AnimState.Dodge;
            if (name.Contains("savus")) return AnimState.Parry;
            if (name.Contains("hasar")) return AnimState.Hurt;
            if (name.Contains("sersem")) return AnimState.Stagger;
            if (name.Contains("olum")) return AnimState.Death;
            return null;
        }

        // Lower case, Turkish letters to plain ones.
        static string Simplify(string s)
        {
            s = s.Replace('İ', 'i').Replace('I', 'ı').ToLowerInvariant();
            return s.Replace('ı', 'i').Replace('ö', 'o').Replace('ü', 'u').Replace('ş', 's').Replace('ğ', 'g').Replace('ç', 'c');
        }
    }
}
