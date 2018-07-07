using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;


namespace shinicht_bot1.Dialogs
{
    [Serializable]
    

    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            await MakeResponce(context,activity);

            context.Wait(MessageReceivedAsync);
        }

        private async Task MakeResponce(IDialogContext context, Activity activity)
        {
            bool bIknow;
            
            String res= "";

            var connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
            //var members = await connector.Conversations.GetConversationMembersAsync(context.Activity.Conversation.Id);



            // calculate something for us to return
            //int length = (activity.Text ?? string.Empty).Length;
            bIknow = false;
            TimeZoneInfo jpTimezoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

            DateTime oDateTime = TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.UtcNow, jpTimezoneInfo);

            activity.Text = Microsoft.Bot.Connector.Teams.ActivityExtensions.GetTextWithoutMentions(activity);

            //Commands check
            switch (activity.Text.ToLower())
            {
                case "/members":
                    {
                        // Fetch the members in the current conversation
                        //var connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
                        //members = await connector.Conversations.GetTeamsConversationMembersAsync(context.Activity.Conversation.Id);
                        var members =  await connector.Conversations.GetConversationMembersAsync(context.Activity.Conversation.Id);

                        // Concatenate information about all members into a string
                        var sbm = new StringBuilder();

                        sbm.AppendLine("こっちゃあるチームのメンバーは、こごだっす");

                        foreach (var member in members.AsTeamsChannelAccounts())
                        {
                            sbm.AppendLine(Environment.NewLine);
                            sbm.AppendFormat(
                                "Name = {0} {1} , upn {2}",
                                member.GivenName, member.Surname, member.UserPrincipalName);

                            // sb.AppendLine();
                        }

                        // Post the member info back into the conversation
                        await context.PostAsync(sbm.ToString());
                        return;
                    }
                    

                case "/channels":
                    ConversationList channels = connector.GetTeamsConnectorClient().Teams.FetchChannelList(activity.GetChannelData<TeamsChannelData>().Team.Id);
                    var sbc = new StringBuilder();


                    sbc.AppendLine("こっちゃあるチームにあるチャンネルは以下だっす");
                    foreach( var channel in channels.Conversations.ToList() )
                    {
                        sbc.AppendLine(Environment.NewLine);
                        string name = channel.Name;
                        if(name == null)
                            name = "Generic";
                        sbc.AppendFormat("Name = {0}",name);
                        
                    }

                    await context.PostAsync(sbc.ToString());
                    return;

                case "/help":
                    {
                        Activity replyToConversation = activity.CreateReply("Should go to conversation, in carousel format");
                        replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                        replyToConversation.Attachments = new List<Attachment>();

                        Dictionary<string, string> cardContentList = new Dictionary<string, string>();
                        cardContentList.Add("PigLatin", "http://www.51sports.net/photos/client_herocard_designs/HC_Goess_front.jpg");
                        cardContentList.Add("Pork Shoulder", "http://www.51sports.net/photos/client_herocard_designs/HC_Goess_front.jpg");
                        cardContentList.Add("Bacon", "http://www.51sports.net/photos/client_herocard_designs/HC_Goess_front.jpg");

                        foreach (KeyValuePair<string, string> cardContent in cardContentList)
                        {
                            List<CardImage> cardImages = new List<CardImage>();

                            cardImages.Add(new CardImage(url: cardContent.Value));

                            List<CardAction> cardButtons = new List<CardAction>();

                            CardAction plButton = new CardAction()
                            {
                                Value = $"https://en.wikipedia.org/wiki/{cardContent.Key}",
                                Type = "openUrl",
                                Title = "WikiPedia Page"
                            };

                            cardButtons.Add(plButton);

                            HeroCard plCard = new HeroCard()
                            {
                                Title = $"I'm a hero card about {cardContent.Key}",
                                Subtitle = $"{cardContent.Key} Wikipedia Page",
                                Images = cardImages,
                                Buttons = cardButtons
                            };

                            Attachment plAttachment = plCard.ToAttachment();
                            replyToConversation.Attachments.Add(plAttachment);
                        }

                        // var reply = 
                        await connector.Conversations.SendToConversationAsync(replyToConversation);

                    }
                    return;

                    
                default:
                    break;
            }

            for (int Index = 0; Index < KnownWords.Length; Index++)
            {
                //activity.Text = Microsoft.Bot.Connector.Teams.ActivityExtensions.GetTextWithoutMentions(activity);
                if (activity.Text.Contains(KnownWords[Index]))
                {
                    if (KnownResps[Index] == (int)RespType.GREETING)
                    {
                        if (oDateTime.TimeOfDay.Hours >= 23)
                            res = "もうこんな時間！早くお休みください。おやすみなさい";
                        else if (oDateTime.TimeOfDay.Hours >= 18)
                            res = "こんばんは！";
                        else if (oDateTime.TimeOfDay.Hours >= 12)
                            res = "こんにちは！";
                        else if (oDateTime.TimeOfDay.Hours >= 6)
                            res = "おはようございます！今日も頑張りましょう！";
                        else if (oDateTime.TimeOfDay.Hours >= 4)
                            res = "おはようございます！ずいぶん早起きですね！まさか、徹夜ですか？";
                        else
                            res = "もうこんな時間！早くお休みください。おやすみなさい";



                    }
                    if (KnownResps[Index] == (int)RespType.TIME)
                    {
                        res = "今の時刻は " + oDateTime.TimeOfDay.ToString() + " だとおもう...";
                    }
                    if (KnownResps[Index] == (int)RespType.TENKI)
                    {


                        res = "なんだって？　" +  activity.Text + "だって?  ほんたらこと、神様にでも聞いてけれ";
                    }
                    bIknow = true;
                }
                
            }
            if (bIknow == false)
            {
                res = ResponceWords[ oDateTime.TimeOfDay.Seconds % ResponceWords.Length];
                
            }



            // return our reply to the user
            // var connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
            Activity replyActivity = activity.CreateReply();
            replyActivity.Text = "さん! " +  res;
            replyActivity.AddMentionToText(activity.From, MentionTextLocation.PrependText);

            //return context.PostAsync(res);
            await connector.Conversations.ReplyToActivityAsync(replyActivity);
        }
        private string[] ResponceWords = {
            "おめ、あんべわりいか",
            "いだましか",
           "なんだこの、くされたまぐらが！",
           "今日は、ええ天気だな",
           "どこに行きっぺか",
           "おれも、おめさんが好きだス",
           "結婚してけれ",
           "あっこの家のあんちゃ、遊んでばりいで、かまどけぁした",
           "服かっちゃまに着てしまった",
           "しぇんしぇの家さ行ぐなさ、からちらでだば行がえねぁ"




        };
        private string[] KnownWords =
        {
            "おはよう",
            "こんにち",
            "こんばん",
            "おやすみ",
            "時間",
            "時刻",
            "なん時",
            "何時",
            "天気",
            "晴れ",
            "雨"
        };
        private enum RespType
        {
            GREETING,
            TIME,
            TENKI
        }

               

        private int[] KnownResps =
        {
            (int)RespType.GREETING,
            (int)RespType.GREETING,
            (int)RespType.GREETING,
            (int)RespType.GREETING,
            (int)RespType.TIME,
            (int)RespType.TIME,
            (int)RespType.TIME,
            (int)RespType.TIME,
            (int)RespType.TENKI,
            (int)RespType.TENKI,
            (int)RespType.TENKI,
            (int)RespType.TENKI


        };
        private string[] Greetings =
{
            "おはようございます！",
            "こんにちは！",
            "こんばんは～",
            "おやすみなさい",
            "まだ時間はわからないの",
            "まだ時刻はわからないの"
        };

    }
}