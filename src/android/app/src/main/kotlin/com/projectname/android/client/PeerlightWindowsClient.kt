package com.projectname.android.client

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.DataInputStream
import java.io.DataOutputStream
import java.net.InetSocketAddress
import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets
import java.security.SecureRandom
import java.security.cert.X509Certificate
import java.util.Locale
import java.util.UUID
import java.util.concurrent.atomic.AtomicLong
import javax.net.ssl.SSLContext
import javax.net.ssl.SSLSocket
import javax.net.ssl.TrustManager
import javax.net.ssl.X509TrustManager

/**
 * Android client for Peerlight Windows Desktop Endpoint.
 * Communicates over the TLS control channel (default port 8443) with length-prefixed JSON frames,
 * delivering normalized pointer movements, mouse button events, and keyboard commands.
 */
class PeerlightWindowsClient {

    companion object {
        const val DEFAULT_PORT = 8443
        const val MAX_FRAME_SIZE = 65536 // 64 KB per spec

        fun createEnvelope(type: String, payloadJson: String, sequenceNumber: Long): String {
            val messageId = UUID.randomUUID().toString()
            val now = System.currentTimeMillis()
            return """{"protocol_version":"1.0","message_id":"$messageId","sequence_number":$sequenceNumber,"timestamp_utc_ms":$now,"type":"$type","payload":$payloadJson}"""
        }

        fun frameMessage(json: String): ByteArray {
            val payloadBytes = json.toByteArray(StandardCharsets.UTF_8)
            val buffer = ByteBuffer.allocate(4 + payloadBytes.size)
            buffer.putInt(payloadBytes.size)
            buffer.put(payloadBytes)
            return buffer.array()
        }
    }

    private var sslSocket: SSLSocket? = null
    private var outStream: DataOutputStream? = null
    private var inStream: DataInputStream? = null
    private val sequenceCounter = AtomicLong(1)

    var isConnected: Boolean = false
        private set
    var targetHost: String? = null
        private set

    /**
     * Connects to a Windows Desktop endpoint over TLS.
     */
    suspend fun connect(host: String, port: Int = DEFAULT_PORT): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            disconnect()

            val trustAll = arrayOf<TrustManager>(object : X509TrustManager {
                override fun checkClientTrusted(chain: Array<X509Certificate>?, authType: String?) {}
                override fun checkServerTrusted(chain: Array<X509Certificate>?, authType: String?) {}
                override fun getAcceptedIssuers(): Array<X509Certificate> = arrayOf()
            })

            val sslContext = SSLContext.getInstance("TLS")
            sslContext.init(null, trustAll, SecureRandom())

            val socket = sslContext.socketFactory.createSocket() as SSLSocket
            socket.connect(InetSocketAddress(host, port), 4000)
            socket.soTimeout = 5000
            socket.startHandshake()

            sslSocket = socket
            outStream = DataOutputStream(socket.outputStream)
            inStream = DataInputStream(socket.inputStream)
            targetHost = host
            isConnected = true
            sequenceCounter.set(1)

            Result.success(Unit)
        } catch (e: Exception) {
            disconnect()
            Result.failure(e)
        }
    }

    /**
     * Dispatches normalized mouse pointer move coordinates [0.0, 1.0].
     */
    suspend fun sendPointerMove(xNorm: Float, yNorm: Float): Result<Unit> = withContext(Dispatchers.IO) {
        val clampedX = xNorm.coerceIn(0f, 1f)
        val clampedY = yNorm.coerceIn(0f, 1f)
        val payload = String.format(Locale.US, """{"x_norm":%.4f,"y_norm":%.4f}""", clampedX, clampedY)
        sendEnvelope("INPUT_POINTER_MOVE", payload)
    }

    /**
     * Dispatches pointer button actions ("click", "down", "up" for "left" or "right").
     */
    suspend fun sendPointerButton(button: String = "left", action: String = "click"): Result<Unit> = withContext(Dispatchers.IO) {
        val payload = """{"button":"$button","action":"$action"}"""
        sendEnvelope("INPUT_POINTER_BUTTON", payload)
    }

    /**
     * Dispatches a keystroke event to Windows desktop.
     */
    suspend fun sendKey(keyCode: String, action: String = "click"): Result<Unit> = withContext(Dispatchers.IO) {
        val payload = """{"key_code":"$keyCode","action":"$action"}"""
        sendEnvelope("INPUT_KEY", payload)
    }

    private fun sendEnvelope(type: String, payloadJson: String): Result<Unit> {
        val out = outStream
        if (out == null || !isConnected) {
            return Result.failure(IllegalStateException("Not connected to Windows endpoint"))
        }

        return try {
            val json = createEnvelope(type, payloadJson, sequenceCounter.getAndIncrement())
            val frame = frameMessage(json)
            synchronized(this) {
                out.write(frame)
                out.flush()
            }
            Result.success(Unit)
        } catch (e: Exception) {
            disconnect()
            Result.failure(e)
        }
    }

    fun disconnect() {
        try {
            outStream?.close()
            inStream?.close()
            sslSocket?.close()
        } catch (_: Exception) {}
        outStream = null
        inStream = null
        sslSocket = null
        isConnected = false
        targetHost = null
    }
}
