package com.projectname.android

import com.projectname.android.client.PeerlightWindowsClient
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets

class PeerlightWindowsClientTest {

    @Test
    fun createEnvelope_generatesValidProtocolFormat() {
        val payload = """{"x_norm":0.5,"y_norm":0.75}"""
        val jsonStr = PeerlightWindowsClient.createEnvelope("INPUT_POINTER_MOVE", payload, 42)

        assertTrue(jsonStr.contains(""""protocol_version":"1.0""""))
        assertTrue(jsonStr.contains(""""type":"INPUT_POINTER_MOVE""""))
        assertTrue(jsonStr.contains(""""sequence_number":42"""))
        assertTrue(jsonStr.contains(""""message_id":"""))
        assertTrue(jsonStr.contains(""""timestamp_utc_ms":"""))
        assertTrue(jsonStr.contains(""""payload":{"x_norm":0.5,"y_norm":0.75}"""))
    }

    @Test
    fun frameMessage_producesFourByteLengthPrefixHeader() {
        val json = """{"type":"PING"}"""
        val framed = PeerlightWindowsClient.frameMessage(json)

        assertNotNull(framed)
        val expectedPayloadLength = json.toByteArray(StandardCharsets.UTF_8).size
        assertEquals(4 + expectedPayloadLength, framed.size)

        val buffer = ByteBuffer.wrap(framed)
        val headerLength = buffer.getInt()
        assertEquals(expectedPayloadLength, headerLength)

        val payloadBytes = ByteArray(headerLength)
        buffer.get(payloadBytes)
        val decodedJson = String(payloadBytes, StandardCharsets.UTF_8)
        assertEquals(json, decodedJson)
    }
}
