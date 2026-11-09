// AUTO-GENERATED from the user's FINAL five-day story script (verbatim EN/VI/KO).
// Do not hand-edit dialogue: wording, slang, spacing and punctuation are intentional.
namespace TwoLadles.Story
{
    public struct StoryLine
    {
        public readonly string speaker; // "jiwon" or "linh"
        public readonly string en, vi, ko;
        public StoryLine(string speaker, string en, string vi, string ko) { this.speaker = speaker; this.en = en; this.vi = vi; this.ko = ko; }
        public string Get(string lang) { return lang == "vi" ? vi : (lang == "ko" ? ko : en); }
    }

    public class DayStory
    {
        public int day;
        public StoryLine[] chat;
        public string dishId, nameEn, nameVi, nameKo;
        public string presenter; // "jiwon", "linh" or "both"
        public string origin;    // "korea", "vietnam" or "fusion"
        public string descEn, descVi, descKo;
        public string GetName(string lang) { return lang == "vi" ? nameVi : (lang == "ko" ? nameKo : nameEn); }
        public string GetDesc(string lang) { return lang == "vi" ? descVi : (lang == "ko" ? descKo : descEn); }
    }

    public static class StoryScript
    {
        public static readonly StoryLine[] Opening =
        {
            new StoryLine("jiwon", "Linh? Still chasing bugs at 2 in the morning?", "Linh à? Cậu vẫn sửa bugs đến tận 2 giờ sáng sao?", "린, 아직도 새벽 2시까지 버그 잡고 있어?"),
            new StoryLine("linh", "Not anymore. I'm trying this new thing called... sleep. :)", "Không. Tui đang thử một thứ mới gọi là... ngủ.", "이젠 아니야. 요즘 '잠'이라는 걸 새로 시도 중이야."),
            new StoryLine("jiwon", "Haha. Anyway, could you give me a hand at my little restaurant? We're only selling tteokbokki for now.", "Haha. Êy mà cậu đến giúp tớ ở quán được không? Hiện giờ quán chỉ bán tteokbokki thôi.", "하하. 그나저나 우리 작은 식당에 와서 좀 도와줄래? 지금은 떡볶이만 팔고 있어."),
            new StoryLine("linh", "Just one dish? Sounds easy enough.", "Một món thôi à? Nghe cũng ổn đấy.", "메뉴가 하나뿐이야? 그 정도면 괜찮겠는데."),
            new StoryLine("jiwon", "That's what I thought. Then the sauce went and beat me up a couple of times.", "Tui cũng nghĩ thế. Rồi nước sốt nó hành tui hai ba lần.", "나도 그렇게 생각했지. 근데 소스한테 두세 번이나 제대로 당했어."),
            new StoryLine("linh", "Send me the address. I'll bring my brain. Maybe a fire extinguisher too .", "Gửi địa chỉ đi. Tui sẽ mang theo não. Cả bình chữa cháy nữa .", "주소 보내. 내 머리 좀 써볼게. 소화기도 챙겨가는 게 좋겠지? "),
            new StoryLine("jiwon", "Deal. I'll save you a clean apron.", "Chốt nhé. Tớ sẽ để dành cho cậu chiếc tạp dề sạch.", "좋아. 깨끗한 앞치마 하나 남겨둘게."),
            new StoryLine("linh", "If it's not clean, I'm not working.", "Ko sạch ko làm nha.", "안 깨끗하면 일 안 할 거야.")
        };

        public static readonly DayStory[] Days =
        {
            new DayStory
            {
                day = 1, dishId = "tteokbokki", presenter = "jiwon", origin = "korea",
                nameEn = "Tteokbokki", nameVi = "Tteokbokki", nameKo = "떡볶이",
                descEn = "A beloved Korean street food made with chewy rice cakes simmered in a sweet and spicy gochujang sauce. Fish cakes are a popular addition.",
                descVi = "Tteokbokki là món ăn đường phố nổi tiếng của Hàn Quốc, làm từ bánh gạo dẻo nấu trong sốt gochujang cay ngọt. Chả cá là một nguyên liệu thường được thêm vào.",
                descKo = "떡볶이는 쫄깃한 떡을 달콤하고 매콤한 고추장 소스에 졸여 만드는 한국의 대표적인 길거리 음식이야. 어묵을 같이 넣어 먹기도 해.",
                chat = new StoryLine[0]
            },
            new DayStory
            {
                day = 2, dishId = "bot_chien", presenter = "linh", origin = "vietnam",
                nameEn = "Bột Chiên", nameVi = "Bột Chiên", nameKo = "봇찌엔",
                descEn = "A popular Saigon street snack made from rice-flour cakes, cut into cubes and pan-fried with egg. It's crispy outside, soft inside, and often served with pickled green papaya.",
                descVi = "Bột chiên là món ăn vặt quen thuộc ở Sài Gòn, làm từ bánh bột gạo cắt miếng rồi chiên trên chảo cùng trứng. Bên ngoài giòn, bên trong mềm, thường ăn kèm đu đủ xanh ngâm chua.",
                descKo = "봇찌엔은 사이공에서 인기 있는 길거리 간식이야. 쌀가루로 만든 떡을 네모나게 잘라 달걀과 함께 팬에 부쳐 먹지. 겉은 바삭하고 속은 부드러우며, 보통 절인 풋파파야를 곁들여 먹어.",
                chat = new StoryLine[]
                {
                    new StoryLine("linh", "You know, I used to be a programmer. Bugs all day, deadlines all night.", "Cậu biết không, trước đây tớ là lập trình viên. Cả ngày lỗi và deadline.", "있잖아, 나 예전엔 개발자였어. 하루 종일 버그 잡고 마감에 쫓겼지."),
                    new StoryLine("jiwon", "I worked in corporate operations. My old boss acted like the world was ending if you didn't reply to an email.", "Tớ thì làm quản lý vận hành ở công ty. Sếp cũ của tớ nghĩ email nào ko rep cx cháy nhà :,).", "난 회사에서 운영 쪽 일을 했어. 전 직장 상사는 이메일 답장이 조금만 늦어도 세상이 끝나는 줄 알았지. :"),
                    new StoryLine("linh", "No wonder you ran off to this little restaurant.", "Bảo sao cậu lại trốn về đây.", "어쩐지 이 작은 식당으로 도망쳐 왔구나."),
                    new StoryLine("jiwon", "Ran off? I prefer \"strategic career change.\" ", "Trốn á? Tui thà gọi đó là \"chuyển hướng sự nghiệp có chiến lược\" hơn.", "도망이라니? 난 '전략적인 커리어 전환'이라고 부를래. "),
                    new StoryLine("jiwon", "We've been selling the same thing for a while. Think we should add something new?", "Êy mà tụi mình bán mãi một món rồi. Cậu nghĩ nên thêm món mới không?", "우리 계속 같은 메뉴만 팔았잖아. 이제 새 메뉴 하나 추가할까?"),
                    new StoryLine("linh", "I've got something in mind. Something I used to eat back in Saigon.", "Tớ có một món trong đầu rồi. Món mà hồi ở Sài Gòn mình hay ăn.", "생각해 둔 게 있어. 사이공에 살 때 내가 자주 먹던 음식이야."),
                    new StoryLine("jiwon", "Alright. You're the expert, so introduce me to it.", "Được thôi. Vậy cậu giới thiệu món đó nhé.", "좋아. 네가 전문가니까 나한테 소개해 줘.")
                }
            },
            new DayStory
            {
                day = 3, dishId = "korean_bbq_rice", presenter = "jiwon", origin = "korea",
                nameEn = "Korean BBQ on Rice", nameVi = "Cơm bò nướng kiểu Hàn", nameKo = "한국식 불고기 덮밥",
                descEn = "Today's dish takes inspiration from Korean bulgogi. Thinly sliced beef is marinated in a sweet and savory sauce, grilled, and served over warm rice.",
                descVi = "Món cơm bò hôm nay lấy cảm hứng từ bulgogi Hàn Quốc, với thịt bò thái mỏng được ướp sốt mặn ngọt, sau đó nướng và ăn cùng cơm nóng.",
                descKo = "오늘 메뉴는 한국의 불고기에서 영감을 받은 덮밥이야. 얇게 썬 소고기를 달콤짭짤한 양념에 재운 뒤 구워서 따뜻한 밥 위에 올려 먹어.",
                chat = new StoryLine[]
                {
                    new StoryLine("jiwon", "This place wears me out too.", "Làm ở quán cũng mệt chết.", "여기서 일하는 것도 진짜 힘드네."),
                    new StoryLine("linh", "But it's a different kind of tired, right?", "Nhưng mệt khác, đúng không?", "그래도 예전이랑은 다른 피곤함이지?"),
                    new StoryLine("jiwon", "Yeah. At the end of the day, we can actually see what we've made. And people genuinely enjoy it.", "Ừ. Cuối ngày, mình cx thấy thành quả của mình. Và mọi người thật sự thưởng thức vài món.", "응. 하루가 끝나면 우리가 뭘 만들었는지 눈에 보이잖아. 사람들도 맛있게 먹어주고."),
                    new StoryLine("linh", "That's a much better performance review.", "Như v đánh giá công việc tốt hơn nhiều đấy.", "그게 훨씬 나은 업무 평가네."),
                    new StoryLine("jiwon", "You get five stars.", "Cậu được năm sao.", "너한테 별 다섯 개 줄게."),
                    new StoryLine("jiwon", "We should add something more filling. Something I grew up eating in Korea.", "Chúng mình nên thêm một món no bụng hơn. Một món mà tớ đã quen ăn từ nhỏ ở Hàn Quốc.", "좀 더 든든한 메뉴를 추가하자. 내가 어릴 때부터 한국에서 즐겨 먹던 음식으로."),
                    new StoryLine("linh", "You're thinking Korean BBQ on rice?", "Cậu đang nghĩ đến cơm bò nướng kiểu Hàn à?", "한국식 불고기 덮밥을 생각하는 거야?"),
                    new StoryLine("jiwon", "Exactly. Leave this one to me.", "Chính xác. Món này cứ để tớ lo.", "맞아. 이 메뉴는 내가 맡을게.")
                }
            },
            new DayStory
            {
                day = 4, dishId = "viet_broken_rice", presenter = "linh", origin = "vietnam",
                nameEn = "Cơm Tấm", nameVi = "Cơm Tấm", nameKo = "껌떰",
                descEn = "Cơm Tấm is a well-loved Saigon specialty made with broken rice, traditionally served with grilled pork, egg and fish sauce. Our restaurant is giving this familiar dish its own little twist.",
                descVi = "Cơm tấm là đặc sản quen thuộc của Sài Gòn, nấu từ gạo tấm và thường ăn cùng sườn nướng, trứng và nước mắm. Nhà hàng của chúng ta sẽ biến tấu món ăn quen thuộc này theo cách riêng.",
                descKo = "껌떰은 부서진 쌀로 지은 밥을 사용하는 사이공의 대표적인 음식이야. 전통적으로 구운 돼지고기와 달걀, 느억맘 소스를 곁들여 먹는데, 우리 식당만의 방식으로 조금 다르게 만들어 볼 거야.",
                chat = new StoryLine[]
                {
                    new StoryLine("linh", "You remembered I don't like it too spicy.", "Cậu nhớ là tớ thích ít cay à?", "내가 너무 매운 건 별로 안 좋아하는 거 기억했네?"),
                    new StoryLine("jiwon", "I remember the important stuff.", "Tớ nhớ những điều quan trọng chứ.", "중요한 건 기억하지."),
                    new StoryLine("linh", "Is that a business habit?", "Đó là thói quen của dân kinh doanh à?", "그게 사업가의 습관이야?"),
                    new StoryLine("jiwon", "Let's call it good customer service.", "Cứ xem như đó là dịch vụ khách hàng chu đáo đi.", "그냥 좋은 고객 서비스라고 해두자."),
                    new StoryLine("linh", "I'm not a customer.", "Tớ đâu phải khách.", "난 손님이 아닌데."),
                    new StoryLine("jiwon", "I know.", "Tớ biết.", "나도 알아."),
                    new StoryLine("linh", "We should add something from home. Cơm Tấm.", "Chúng ta nên thêm một món quê nhà của tớ. Cơm tấm nhé.", "내 고향 음식도 하나 추가하자. 껌떰 어때?"),
                    new StoryLine("jiwon", "Will you teach me how to make it?", "Dạy tớ cách làm nhé?", "어떻게 만드는지 나한테 가르쳐 줄래?"),
                    new StoryLine("linh", "Only if you promise not to burn the pork.", "Chỉ khi cậu hứa không nướng cháy thịt thôi.", "돼지고기 안 태우겠다고 약속하면.")
                }
            },
            new DayStory
            {
                day = 5, dishId = "bulgogi_banh_mi", presenter = "both", origin = "fusion",
                nameEn = "Bulgogi Bánh Mì", nameVi = "Bulgogi Bánh Mì", nameKo = "불고기 반미",
                descEn = "Our own Vietnam–Korea fusion creation: Korean-style marinated beef tucked inside a crunchy Vietnamese baguette, topped with fresh, tangy pickled vegetables.",
                descVi = "Món kết hợp Việt–Hàn do nhà hàng chúng ta sáng tạo, kết hợp thịt bò ướp kiểu Hàn Quốc với bánh mì Việt Nam giòn cùng rau củ ngâm chua.",
                descKo = "불고기 반미는 우리 가게가 직접 만든 베트남–한국 퓨전 메뉴야. 한국식 양념 소고기를 바삭한 베트남 바게트에 넣고 새콤한 절임 채소를 더했어.",
                chat = new StoryLine[]
                {
                    new StoryLine("jiwon", "Big day. Nervous?", "Ngày trọng đại đấy. có hồi hộp không?", "Ngày trọng đại rồi đấy. 긴장돼?"),
                    new StoryLine("linh", "A little. But I'm glad we're doing this together.", "Một chút. Nhưng em vui vì mình chung tay mà làm.", "조금. 그래도 우리가 함께하고 있어서 좋아."),
                    new StoryLine("jiwon", "I was thinking... what if we combined something from both our food cultures?", "Anh đang nghĩ... hay là chúng ta kết hợp món ăn từ cả hai nền ẩm thực nhé?", "문득 생각했는데... 우리 두 나라의 음식을 하나로 합쳐보는 건 어때?"),
                    new StoryLine("linh", "Kind of like bulgogi in a Vietnamese bánh mì, huh?", "Kiểu như Bulgogi kẹp trong bánh mì Việt Nam à?", "베트남 반미에 불고기를 넣는 거네?"),
                    new StoryLine("jiwon", "Add some crunchy pickled vegetables, too. Our own little specialty.", "Thêm chút rau củ muối chua giòn nữa. Món đặc biệt của riêng của shop.", "아삭한 절임 채소도 넣자. 우리 가게만의 특별 메뉴로."),
                    new StoryLine("linh", "I like it. Let's make something people will remember.", "Em thấy oke đấy. Hãy làm một món mà mọi người sẽ nhớ đến.", "마음에 들어. 사람들이 오래 기억할 만한 메뉴로 만들자."),
                    new StoryLine("jiwon", "Me too. Dinner after we close? No more work talk. I'm tired of all that.", "anh cũng vậy. Sau khi đóng cửa, đi ăn nhé? Không bàn chuyện công việc nx mệt r.", "나도 그래. 가게 문 닫고 저녁 먹을래? 일 얘기는 그만하자. 이제 좀 지쳤어."),
                    new StoryLine("linh", "Deal. But I'm picking dessert.", "Đồng ý. Nhưng món tráng miệng để em chọn nhé.", "좋아. 대신 디저트는 내가 고를게."),
                    new StoryLine("jiwon", "Go ahead. Pick whatever you like.", "Ừ chọn đi.<3", "그래, 네가 골라. <3")
                }
            }
        };

        /// <summary>Returns the story for campaign day 1..5, or null.</summary>
        public static DayStory GetDay(int day) { return (day >= 1 && day <= Days.Length) ? Days[day - 1] : null; }
    }
}
