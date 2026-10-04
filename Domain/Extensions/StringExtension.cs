using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace Domain.Extensions;

public static class StringExtension
{
    // Precomposed Latin, Greek and Cyrillic letters (U+00C0-U+1FFF) and their base letter, i.e. the result of
    // "FormD then drop the non-spacing marks". A table rather than string.Normalize, which needs ICU: it is a no-op
    // in globalization-invariant mode (chiseled images), and library folder names must not depend on the host.
    private const string Accented =
        "ÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝàáâãäåçèéêëìíîïñòóôõöù"
        + "úûüýÿĀāĂăĄąĆćĈĉĊċČčĎďĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĨĩĪīĬĭĮ"
        + "įİĴĵĶķĹĺĻļĽľŃńŅņŇňŌōŎŏŐőŔŕŖŗŘřŚśŜŝŞşŠšŢţŤťŨũŪūŬŭ"
        + "ŮůŰűŲųŴŵŶŷŸŹźŻżŽžƠơƯưǍǎǏǐǑǒǓǔǕǖǗǘǙǚǛǜǞǟǠǡǢǣǦǧǨǩǪ"
        + "ǫǬǭǮǯǰǴǵǸǹǺǻǼǽǾǿȀȁȂȃȄȅȆȇȈȉȊȋȌȍȎȏȐȑȒȓȔȕȖȗȘșȚțȞȟȦȧ"
        + "ȨȩȪȫȬȭȮȯȰȱȲȳʹΆΈΉΊΌΎΏΐΪΫάέήίΰϊϋόύώϓϔЀЁЃЇЌЍЎЙйѐёѓї"
        + "ќѝўѶѷӁӂӐӑӒӓӖӗӚӛӜӝӞӟӢӣӤӥӦӧӪӫӬӭӮӯӰӱӲӳӴӵӸӹḀḁḂḃḄḅḆḇḈ"
        + "ḉḊḋḌḍḎḏḐḑḒḓḔḕḖḗḘḙḚḛḜḝḞḟḠḡḢḣḤḥḦḧḨḩḪḫḬḭḮḯḰḱḲḳḴḵḶḷḸ"
        + "ḹḺḻḼḽḾḿṀṁṂṃṄṅṆṇṈṉṊṋṌṍṎṏṐṑṒṓṔṕṖṗṘṙṚṛṜṝṞṟṠṡṢṣṤṥṦṧṨ"
        + "ṩṪṫṬṭṮṯṰṱṲṳṴṵṶṷṸṹṺṻṼṽṾṿẀẁẂẃẄẅẆẇẈẉẊẋẌẍẎẏẐẑẒẓẔẕẖẗẘ"
        + "ẙẛẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọ"
        + "ỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹἀἁἂἃ"
        + "ἄἅἆἇἈἉἊἋἌἍἎἏἐἑἒἓἔἕἘἙἚἛἜἝἠἡἢἣἤἥἦἧἨἩἪἫἬἭἮἯἰἱἲἳἴἵἶἷ"
        + "ἸἹἺἻἼἽἾἿὀὁὂὃὄὅὈὉὊὋὌὍὐὑὒὓὔὕὖὗὙὛὝὟὠὡὢὣὤὥὦὧὨὩὪὫὬὭὮὯ"
        + "ὰάὲέὴήὶίὸόὺύὼώᾀᾁᾂᾃᾄᾅᾆᾇᾈᾉᾊᾋᾌᾍᾎᾏᾐᾑᾒᾓᾔᾕᾖᾗᾘᾙᾚᾛᾜᾝᾞᾟᾠᾡ"
        + "ᾢᾣᾤᾥᾦᾧᾨᾩᾪᾫᾬᾭᾮᾯᾰᾱᾲᾳᾴᾶᾷᾸᾹᾺΆᾼιῂῃῄῆῇῈΈῊΉῌῐῑῒΐῖῗῘῙῚΊῠ"
        + "ῡῢΰῤῥῦῧῨῩῪΎῬῲῳῴῶῷῸΌῺΏῼ";

    private const string Unaccented =
        "AAAAAACEEEEIIIINOOOOOUUUUYaaaaaaceeeeiiiinooooou"
        + "uuuyyAaAaAaCcCcCcCcDdEeEeEeEeEeGgGgGgGgHhIiIiIiI"
        + "iIJjKkLlLlLlNnNnNnOoOoOoRrRrRrSsSsSsSsTtTtUuUuUu"
        + "UuUuUuWwYyYZzZzZzOoUuAaIiOoUuUuUuUuUuAaAaÆæGgKkO"
        + "oOoƷʒjGgNnAaÆæØøAaAaEeEeIiIiOoOoRrRrUuUuSsTtHhAa"
        + "EeOoOoOoOoYyʹΑΕΗΙΟΥΩιΙΥαεηιυιυουωϒϒЕЕГІКИУИиеегі"
        + "киуѴѵЖжАаАаЕеӘәЖжЗзИиИиОоӨөЭэУуУуУуЧчЫыAaBbBbBbC"
        + "cDdDdDdDdDdEeEeEeEeEeFfGgHhHhHhHhHhIiIiKkKkKkLlL"
        + "lLlLlMmMmMmNnNnNnNnOoOoOoOoPpPpRrRrRrRrSsSsSsSsS"
        + "sTtTtTtTtUuUuUuUuUuVvVvWwWwWwWwWwXxXxYyZzZzZzhtw"
        + "yſAaAaAaAaAaAaAaAaAaAaAaAaEeEeEeEeEeEeEeEeIiIiOo"
        + "OoOoOoOoOoOoOoOoOoOoOoUuUuUuUuUuUuUuYyYyYyYyαααα"
        + "ααααΑΑΑΑΑΑΑΑεεεεεεΕΕΕΕΕΕηηηηηηηηΗΗΗΗΗΗΗΗιιιιιιιι"
        + "ΙΙΙΙΙΙΙΙοοοοοοΟΟΟΟΟΟυυυυυυυυΥΥΥΥωωωωωωωωΩΩΩΩΩΩΩΩ"
        + "ααεεηηιιοουυωωααααααααΑΑΑΑΑΑΑΑηηηηηηηηΗΗΗΗΗΗΗΗωω"
        + "ωωωωωωΩΩΩΩΩΩΩΩαααααααΑΑΑΑΑιηηηηηΕΕΗΗΗιιιιιιΙΙΙΙυ"
        + "υυυρρυυΥΥΥΥΡωωωωωΟΟΩΩΩ";

    private static readonly FrozenDictionary<char, char> s_baseLetters =
        Accented.Zip(Unaccented).ToFrozenDictionary(pair => pair.First, pair => pair.Second);

    public static string RemoveDiacritics(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        var stringBuilder = new StringBuilder(str.Length);
        foreach (var c in str)
        {
            // Already decomposed input: the combining marks are dropped as well.
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(s_baseLetters.GetValueOrDefault(c, c));
            }
        }

        return stringBuilder.ToString();
    }
}
