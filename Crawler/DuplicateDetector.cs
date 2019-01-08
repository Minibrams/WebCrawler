using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Crawler
{
    class DuplicateDetector
    {
        private long[] _permutationNumbers = new long[] 
        {
            6007140162948984919, 8782914162097178112, 770402467438425262,
            8252931203538622552, 5705018868078654231, 357570686568874082,
            8199150926399439056, 4052072967475160791, 1854994339515175984,
            5108003833145539406, 1985874593763824785, 9202466632364135276,
            2498161104934776406, 718033017350783402, 6543554776996810047,
            8473118902812098526, 7779729771381883285, 7685768929430225563,
            3812343741748954754, 2814465008897749890, 8103293770666513113,
            6232788966344592672, 5481691207555038845, 5771189349054000190,
            3700946383525240107, 2102958621446660689, 5535445817824699162,
            2319693978201883669, 2514675819900426491, 6284137387766591317,
            2229942510612679736, 5172072946780092551, 7765850580642100202,
            2399135643672240265, 6433894319551891857, 1418162840435866356,
            9053745147330399445, 8824317463801717136, 5613077667249079945,
            1107034462448110882, 4000951037922226514, 7537332084654522018,
            8765502697113336962, 2572922763288176665, 6691639326965038747,
            4707368280559948801, 15252938208710573, 6491568057879569589,
            8080183682237950589, 8528116097136247951, 4886445658677929857,
            2021393013659766798, 7486001159308939268, 3461605623326267724,
            6837589288001321820, 6975934762907442487, 4339179875458003956,
            735174623205209550, 6033514074793279199, 485895855360336308,
            1180881607042096166, 6847713195490824583, 6560116240345899170,
            2025859376802398935, 830359494134965547, 2130284976225841355,
            774565340669014205, 5003576196040203292, 1776584935318940537,
            8091486110018688790, 2304521256142007457, 8741986132404445604,
            6606635639259508181, 2773805753607177999, 5663951222091683074,
            3742262519130907097, 2813480118024411612, 6304075446649176065,
            4189311044940840230, 198657983897544884, 2930531390887891377,
            4044148963693021255, 4133337069266729416, 1456574658063872144
        };
        public List<long[]> DocumentFingerprints { get; set; }

        public DuplicateDetector()
        {
            DocumentFingerprints = new List<long[]>();
        }

        public long[] GenerateFingerprint(string text)
        {
            text = new string(text.Where(x => char.IsLetter(x) || char.IsDigit(x) || char.IsWhiteSpace(x)).ToArray());
            string[] words = text.Split(' ');
            List<string> shingles = new List<string>();

            for (int i = 0; i < words.Length - 3; i++)
            {
                shingles.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
            }

            // Calculate the hashes for every shingle, pick the minimum hash
            // Repeat 84 times: 
            // - Permute each hash with the same seeds
            // - Pick the minimum one

            long[] fingerPrint = new long[_permutationNumbers.Length];
            long[] hashes = shingles.Select(shingle => GetInt64HashCode(shingle)).ToArray();
            for (int i = 0; i < _permutationNumbers.Length; i++)
            {
                long[] permutedHashes = hashes.Select(hash => hash ^ _permutationNumbers[i]).ToArray();
                fingerPrint[i] = permutedHashes.Min();
            }

            return fingerPrint;
        }

        public long GetInt64HashCode(string strText)
        {
            long hashCode = 0;
            if (!string.IsNullOrEmpty(strText))
            {
                //Unicode Encode Covering all characterset
                byte[] byteContents = Encoding.Unicode.GetBytes(strText);
                System.Security.Cryptography.SHA256 hash =
                new System.Security.Cryptography.SHA256CryptoServiceProvider();
                byte[] hashText = hash.ComputeHash(byteContents);

                long hashCodeStart = BitConverter.ToInt64(hashText, 0);
                long hashCodeMedium = BitConverter.ToInt64(hashText, 8);
                long hashCodeEnd = BitConverter.ToInt64(hashText, 24);
                hashCode = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;
            }
            return hashCode;
        }

        public bool IsDuplicate(string text)
        {
            long[] f1 = GenerateFingerprint(text);
            foreach (long[] f2 in DocumentFingerprints)
            {
                // Calculate Jaccard Similarity between this and every other 
                // fingerprint in the store

                if (JaccardSimilarity(f1, f2) > 0.8)
                {
                    // Duplicate found
                    return true;
                }
            }

            // Not a duplicate, so add to the document fingerprints
            DocumentFingerprints.Add(f1);
            return false;
        }

        public float JaccardSimilarity(long[] f1, long[] f2)
        {
            return f1.Intersect(f2).Count() / f1.Union(f2).Count();
        }
    }
}
