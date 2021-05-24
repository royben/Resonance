
#pragma unmanaged
#include "internals.h"
#include "defaults.h"
#include "conductor.h"
#include "webrtc\api\peerconnectioninterface.h"
#pragma managed

#include "msclr\marshal_cppstd.h"

using namespace System::Runtime::InteropServices;
using namespace System::Reflection;
using namespace System;
using namespace System::IO;
using namespace System::Diagnostics;

using namespace msclr::interop;

[assembly:System::Runtime::Versioning::TargetFrameworkAttribute(L".NETFramework,Version=v4.0", FrameworkDisplayName = L".NET Framework 4")] ;

namespace Internal
{
	void Encode(unsigned char* data, unsigned int size, int part_idx, bool keyFrame)
	{
		//Util::OnFillBuffer(data, size, part_idx, keyFrame);
	}
}

namespace Resonance
{
	namespace WebRTC
	{
		public enum class IceConnectionStates : int
		{
			kIceConnectionNew,
			kIceConnectionChecking,
			kIceConnectionConnected,
			kIceConnectionCompleted,
			kIceConnectionFailed,
			kIceConnectionDisconnected,
			kIceConnectionClosed,
			kIceConnectionMax,
		};

		public ref class ManagedConductor
		{
		private:

			bool m_isDisposed;
			Native::Conductor* cd;

			delegate void _OnRenderCallback(uint8_t* frame_buffer, uint32_t w, uint32_t h);
			_OnRenderCallback^ onRenderLocal;
			_OnRenderCallback^ onRenderRemote;
			GCHandle^ onRenderLocalHandle;
			GCHandle^ onRenderRemoteHandle;

			delegate void _OnErrorCallback();
			_OnErrorCallback^ onError;
			GCHandle^ onErrorHandle;

			delegate void _OnSuccessCallback(String^ type, String^ sdp);
			_OnSuccessCallback^ onSuccess;
			GCHandle^ onSuccessHandle;

			delegate void _OnIceStateChangedCallback(webrtc::PeerConnectionInterface::IceConnectionState state);
			_OnIceStateChangedCallback^ onIceStateChanged;
			GCHandle^ onIceStateChangedHandle;

			delegate void _OnFailureCallback(String^ error);
			_OnFailureCallback^ onFailure;
			GCHandle^ onFailureHandle;

			delegate void _OnDataMessageCallback(String^ msg);
			_OnDataMessageCallback^ onDataMessage;
			GCHandle^ onDataMessageHandle;

			delegate void _OnDataBinaryMessageCallback(uint8_t* msg, uint32_t size);
			_OnDataBinaryMessageCallback^ onDataBinaryMessage;
			GCHandle^ onDataBinaryMessageHandle;

			delegate void _OnIceCandidateCallback(String^ sdp_mid, Int32 sdp_mline_index, String^ sdp);
			_OnIceCandidateCallback^ onIceCandidate;
			GCHandle^ onIceCandidateHandle;

			void FreeGCHandle(GCHandle^% g)
			{
				if (g != nullptr)
				{
					g->Free();
					g = nullptr;
				}
			}

			void _OnError()
			{
				Debug::WriteLine("OnError");

				OnError();
			}

			void _OnSuccess(String^ type, String^ sdp)
			{
				Debug::WriteLine(String::Format("OnSuccess: {0} -> {1}", type, sdp));

				if (type == "offer")
				{
					OnSuccessOffer(sdp);
				}
				else if (type == "answer")
				{
					OnSuccessAnswer(sdp);
				}
			}

			void _OnIceStateChanged(webrtc::PeerConnectionInterface::IceConnectionState state)
			{
				IceConnectionStates managedState = static_cast<IceConnectionStates>(state);
				OnIceStateChanged(managedState);
			}

			void _OnIceCandidate(String^ sdp_mid, Int32 sdp_mline_index, String^ sdp)
			{
				Debug::WriteLine(String::Format("OnIceCandidate: {0}", sdp));

				OnIceCandidate(sdp_mid, sdp_mline_index, sdp);
			}

			void _OnFailure(String^ error)
			{
				Debug::WriteLine(String::Format("OnFailure: {0}", error));

				OnFailure(error);
			}

			void _OnDataMessage(String^ msg)
			{
				OnDataMessage(msg);
			}

			void _OnDataBinaryMessage(uint8_t* data, uint32_t size)
			{
				array<Byte>^ data_array = gcnew array<Byte>(size);

				IntPtr src(data);
				Marshal::Copy(src, data_array, 0, size);

				OnDataBinaryMessage(data_array);
			}

			void _OnRenderLocal(uint8_t* frame_buffer, uint32_t w, uint32_t h)
			{
				OnRenderLocal(frame_buffer, w, h);
			}

			void _OnRenderRemote(uint8_t* frame_buffer, uint32_t w, uint32_t h)
			{
				OnRenderRemote(frame_buffer, w, h);
			}

		public:

			delegate void OnCallbackSdp(String^ sdp);
			event OnCallbackSdp^ OnSuccessOffer;
			event OnCallbackSdp^ OnSuccessAnswer;

			delegate void OnCallbackIceCandidate(String^ sdp_mid, Int32 sdp_mline_index, String^ sdp);
			event OnCallbackIceCandidate^ OnIceCandidate;

			delegate void OnIceStateChangedCallback(IceConnectionStates state);
			event OnIceStateChangedCallback^ OnIceStateChanged;

			event Action^ OnError;

			delegate void OnCallbackError(String^ error);
			event OnCallbackError^ OnFailure;

			delegate void OnCallbackDataMessage(String^ msg);
			event OnCallbackDataMessage^ OnDataMessage;

			delegate void OnCallbackDataBinaryMessage(array<Byte>^ msg);
			event OnCallbackDataBinaryMessage^ OnDataBinaryMessage;

			delegate void OnCallbackRender(System::Byte* frame_buffer, System::UInt32 w, System::UInt32 h);
			event OnCallbackRender^ OnRenderLocal;
			event OnCallbackRender^ OnRenderRemote;

			ManagedConductor()
			{
				m_isDisposed = false;
				cd = new Native::Conductor();

				onRenderLocal = gcnew _OnRenderCallback(this, &ManagedConductor::_OnRenderLocal);
				onRenderLocalHandle = GCHandle::Alloc(onRenderLocal);
				cd->onRenderLocal = static_cast<Native::OnRenderCallbackNative>(Marshal::GetFunctionPointerForDelegate(onRenderLocal).ToPointer());

				onRenderRemote = gcnew _OnRenderCallback(this, &ManagedConductor::_OnRenderRemote);
				onRenderRemoteHandle = GCHandle::Alloc(onRenderRemote);
				cd->onRenderRemote = static_cast<Native::OnRenderCallbackNative>(Marshal::GetFunctionPointerForDelegate(onRenderRemote).ToPointer());

				onError = gcnew _OnErrorCallback(this, &ManagedConductor::_OnError);
				onErrorHandle = GCHandle::Alloc(onError);
				cd->onError = static_cast<Native::OnErrorCallbackNative>(Marshal::GetFunctionPointerForDelegate(onError).ToPointer());

				onSuccess = gcnew _OnSuccessCallback(this, &ManagedConductor::_OnSuccess);
				onSuccessHandle = GCHandle::Alloc(onSuccess);
				cd->onSuccess = static_cast<Native::OnSuccessCallbackNative>(Marshal::GetFunctionPointerForDelegate(onSuccess).ToPointer());

				onIceStateChanged = gcnew _OnIceStateChangedCallback(this, &ManagedConductor::_OnIceStateChanged);
				onIceStateChangedHandle = GCHandle::Alloc(onIceStateChanged);
				cd->onIceStateChanged = static_cast<Native::OnIceStateChangedCallbackNative>(Marshal::GetFunctionPointerForDelegate(onIceStateChanged).ToPointer());

				onFailure = gcnew _OnFailureCallback(this, &ManagedConductor::_OnFailure);
				onFailureHandle = GCHandle::Alloc(onFailure);
				cd->onFailure = static_cast<Native::OnFailureCallbackNative>(Marshal::GetFunctionPointerForDelegate(onFailure).ToPointer());

				onDataMessage = gcnew _OnDataMessageCallback(this, &ManagedConductor::_OnDataMessage);
				onDataMessageHandle = GCHandle::Alloc(onDataMessage);
				cd->onDataMessage = static_cast<Native::OnDataMessageCallbackNative>(Marshal::GetFunctionPointerForDelegate(onDataMessage).ToPointer());

				onDataBinaryMessage = gcnew _OnDataBinaryMessageCallback(this, &ManagedConductor::_OnDataBinaryMessage);
				onDataBinaryMessageHandle = GCHandle::Alloc(onDataBinaryMessage);
				cd->onDataBinaryMessage = static_cast<Native::OnDataBinaryMessageCallbackNative>(Marshal::GetFunctionPointerForDelegate(onDataBinaryMessage).ToPointer());

				onIceCandidate = gcnew _OnIceCandidateCallback(this, &ManagedConductor::_OnIceCandidate);
				onIceCandidateHandle = GCHandle::Alloc(onIceCandidate);
				cd->onIceCandidate = static_cast<Native::OnIceCandidateCallbackNative>(Marshal::GetFunctionPointerForDelegate(onIceCandidate).ToPointer());
			}

			~ManagedConductor()
			{
				if (m_isDisposed)
					return;

				m_isDisposed = true;

				cd->Dispose();

				// dispose managed data
				FreeGCHandle(onErrorHandle);
				FreeGCHandle(onSuccessHandle);
				FreeGCHandle(onIceStateChangedHandle);
				FreeGCHandle(onFailureHandle);
				FreeGCHandle(onIceCandidateHandle);
				FreeGCHandle(onRenderLocalHandle);
				FreeGCHandle(onRenderRemoteHandle);
				FreeGCHandle(onDataMessageHandle);

				//this->!ManagedConductor(); // call finalizer
			}

			static void InitializeSSL()
			{
				Native::InitializeSSL();
			}

			static void CleanupSSL()
			{
				Native::CleanupSSL();
			}

			bool InitializePeerConnection()
			{
				return cd->InitializePeerConnection();
			}

			void CreateOffer()
			{
				cd->CreateOffer();
			}

			bool ProcessMessages(Int32 delay)
			{
				return cd->ProcessMessages(delay);
			}

			bool OpenVideoCaptureDevice(String^ name)
			{
				return cd->OpenVideoCaptureDevice(marshal_as<std::string>(name));
			}

			static System::Collections::Generic::List<String^>^ GetVideoDevices()
			{
				System::Collections::Generic::List<String^>^ ret = nullptr;
				std::vector<std::string> devices = Native::Conductor::GetVideoDevices();
				for (const auto& d : devices)
				{
					if (ret == nullptr)
					{
						ret = gcnew System::Collections::Generic::List<String^>();
					}
					ret->Add(marshal_as<String^>(d));
				}
				return ret;
			}

			void OnOfferReply(String^ type, String^ sdp)
			{
				cd->OnOfferReply(marshal_as<std::string>(type), marshal_as<std::string>(sdp));
			}

			void OnOfferRequest(String^ sdp)
			{
				cd->OnOfferRequest(marshal_as<std::string>(sdp));
			}

			bool AddIceCandidate(String^ sdp_mid, Int32 sdp_mlineindex, String^ sdp)
			{
				return cd->AddIceCandidate(marshal_as<std::string>(sdp_mid), sdp_mlineindex, marshal_as<std::string>(sdp));
			}

			void AddServerConfig(String^ uri, String^ username, String^ password)
			{
				cd->AddServerConfig(marshal_as<std::string>(uri), marshal_as<std::string>(username), marshal_as<std::string>(password));
			}

			void CreateDataChannel(String^ label)
			{
				cd->CreateDataChannel(marshal_as<std::string>(label));
			}

			void DataChannelSendText(String^ text)
			{
				cd->DataChannelSendText(marshal_as<std::string>(text));
			}

			void DataChannelSendData(array<Byte>^ array_data)
			{
				pin_ptr<uint8_t> thePtr = &array_data[0];
				uint8_t* bPtr = thePtr;
				rtc::CopyOnWriteBuffer writeBuffer(bPtr, array_data->Length);
				cd->DataChannelSendData(webrtc::DataBuffer(writeBuffer, true));
			}

			void SetAudio(bool enable)
			{
				cd->audioEnabled = enable;
			}

			void SetVideoCapturer(int width, int height, int caputureFps, bool barcodeEnabled)
			{
				cd->width_ = width;
				cd->height_ = height;
				cd->caputureFps = caputureFps;
				cd->barcodeEnabled = barcodeEnabled;
			}

			System::Byte* VideoCapturerI420Buffer()
			{
				return cd->VideoCapturerI420Buffer();
			}

			void PushFrame()
			{
				cd->PushFrame();
			}

			System::Byte* DesktopCapturerRGBAbuffer()
			{
#if DESKTOP_CAPTURE
				return cd->DesktopCapturerRGBAbuffer();
#else
				throw gcnew System::NotImplementedException("set internals.h #define DESKTOP_CAPTURE 1");
#endif
			}

			void DesktopCapturerSize(Int32% w, Int32% h)
			{
#if DESKTOP_CAPTURE
				int wn = 0, hn = 0;
				cd->DesktopCapturerSize(wn, hn);
				w = wn;
				h = hn;
#else
				throw gcnew System::NotImplementedException("set internals.h #define DESKTOP_CAPTURE 1");
#endif
			}

			void CaptureFrame()
			{
#if DESKTOP_CAPTURE
				cd->CaptureFrame();
#else
				throw gcnew System::NotImplementedException("set internals.h #define DESKTOP_CAPTURE 1");
#endif
			}

#pragma region -- Servers --
			bool RunStunServer(String^ bindIp)
			{
				return cd->RunStunServer(marshal_as<std::string>(bindIp));
			}

			// File is stored as lines of <username>=<HA1>.
			// Generate HA1 via "echo -n "<username>:<realm>:<password>" | md5sum"
			bool RunTurnServer(String^ bindIp, String^ ip, String^ realm, String^ authFile)
			{
				return cd->RunTurnServer(marshal_as<std::string>(bindIp), marshal_as<std::string>(ip), marshal_as<std::string>(realm), marshal_as<std::string>(authFile));
			}
#pragma endregion

		protected:

			!ManagedConductor()
			{
				// free unmanaged data
				//if (cd != NULL)
				//{
				//	delete cd;					
				//}
				//cd = NULL;
			}
		};
	}
}
