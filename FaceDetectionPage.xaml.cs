using Xamarin.Forms;
using System;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Plugin.Connectivity;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System.Threading.Tasks;
using System.Linq;

namespace FaceDetection
{
	public partial class FaceDetectionPage : ContentPage
	{
		private readonly IFaceServiceClient faceServiceClient;
		private readonly EmotionServiceClient emotionServiceClient;
		public FaceDetectionPage()
		{
			InitializeComponent();
			// Provides access to the Face APIs
			this.faceServiceClient = new FaceServiceClient(AppRes.KeyFaceAPI); 
			// Provides access to the Emotion APIs
			this.emotionServiceClient = new EmotionServiceClient(AppRes.KeyEmotionAPI);
		}

		private async void UploadPictureButton_Clicked(object sender, EventArgs e)
		{
			if (!CrossMedia.Current.IsPickPhotoSupported)
			{
				await DisplayAlert("No upload", "Picking a photo is not supported.", "OK");
				return;
			}
			MediaFile file = null;
			System.IO.Stream stream =null;
			this.Indicator1.IsVisible = true;
			this.Indicator1.IsRunning = true;
			await CrossMedia.Current.PickPhotoAsync().ContinueWith(t =>
			{
				if (t.IsCompleted)
				{
					file = t.Result;
					stream = t.Result.GetStream();

				}
			}, TaskScheduler.FromCurrentSynchronizationContext()); 

			if (file == null) return;

			if (stream != null)
			{
				Image1.Source = ImageSource.FromStream(() => stream);
			}
			FaceEmotionDetection theData = await DetectFaceAndEmotionsAsync(file);
			this.BindingContext = theData;
			this.Indicator1.IsRunning = false;
			this.Indicator1.IsVisible = false;
		}

		private async void TakePictureButton_Clicked(object sender, EventArgs e)
		{
			

			if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
			{
				await DisplayAlert("No Camera", "No camera available.", "OK");
				return;
			}

			MediaFile file = null;
			System.IO.Stream stream = null;

			await CrossMedia.Current.Initialize();
			await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
			{
				SaveToAlbum = true,
				Name = "test.jpg"
			}).ContinueWith(t =>
			{
				if (t.IsCompleted)
				{
					file = t.Result;
					stream = t.Result.GetStream();

				}
			}, TaskScheduler.FromCurrentSynchronizationContext());

			if (file == null)
				return;
			this.Indicator1.IsVisible = true;
			this.Indicator1.IsRunning = true;
			Image1.Source = ImageSource.FromStream(() => stream);
			FaceEmotionDetection theData = await DetectFaceAndEmotionsAsync(file);
			this.BindingContext = theData;
			this.Indicator1.IsRunning = false;
			this.Indicator1.IsVisible = false;
		}

		private async Task<FaceEmotionDetection> DetectFaceAndEmotionsAsync(MediaFile inputFile)
		{
			// Create a list of face attributes that the
			// app will need to retrieve
			var requiredFaceAttributes = new FaceAttributeType[] {
																  FaceAttributeType.Age,
																  FaceAttributeType.Gender,
																  FaceAttributeType.Smile,
																  FaceAttributeType.FacialHair,
																  FaceAttributeType.HeadPose,
																  FaceAttributeType.Glasses
																  };
			try
			{
				// Get emotions from the specified stream
				Emotion[] emotionResult = await
				  emotionServiceClient.RecognizeAsync(inputFile.GetStream());
				// Assuming the picture has one face, retrieve emotions for the
				// first item in the returned array
				var faceEmotion = emotionResult[0]?.Scores.ToRankedList();

				// Get a list of faces in a picture
				var faces = await faceServiceClient.DetectAsync(inputFile.GetStream(),
				  false, false, requiredFaceAttributes);
				// Assuming there is only one face, store its attributes
				var faceAttributes = faces[0]?.FaceAttributes;

				FaceEmotionDetection faceEmotionDetection = new FaceEmotionDetection();
				faceEmotionDetection.Age = faceAttributes.Age;
				faceEmotionDetection.Emotion = faceEmotion.FirstOrDefault().Key;
				faceEmotionDetection.Glasses = faceAttributes.Glasses.ToString();
				faceEmotionDetection.Smile = faceAttributes.Smile;
				faceEmotionDetection.Gender = faceAttributes.Gender;
				faceEmotionDetection.Moustache = faceAttributes.FacialHair.Moustache;
				faceEmotionDetection.Beard = faceAttributes.FacialHair.Beard;

				return faceEmotionDetection;
			}
			catch (Exception ex)
			{
				await DisplayAlert("Error", ex.Message, "OK");
				return null;
			}
		}
	}
}
